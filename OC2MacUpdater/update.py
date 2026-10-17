#!/usr/bin/env python3
"""Overcooked! 2 Mac updater. Keeps rollback copies; never executes PowerShell."""
from __future__ import annotations
import argparse, hashlib, json, os, re, shutil, stat, subprocess, sys, tempfile, time, uuid, zipfile, fcntl
from contextlib import contextmanager
from pathlib import Path
TOOL = Path(__file__).resolve().parent
REPO = 'CH3NGYZ/Overcooked-2-HostUtilities-Stable-Releases'

def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''): h.update(block)
    return h.hexdigest()

def version(text):
    s = str(text).removeprefix('v')
    if not re.fullmatch(r'\d+\.\d+\.\d+(?:\.\d+)?', s): raise ValueError('Invalid version: ' + s)
    v = tuple(map(int, s.split('.')))
    return v + (0,) * (4-len(v))

def canonical(path):
    p = Path(os.path.abspath(path))
    for q in [p, *p.parents]:
        if q.is_symlink(): raise ValueError('拒绝符号链接: ' + str(q))
    return p

def tree_check(path):
    p = canonical(path)
    if p.exists():
        for base, dirs, files in os.walk(p, followlinks=False):
            for name in dirs + files:
                q = Path(base)/name
                if q.is_symlink() or (q.exists() and not (q.is_dir() or q.is_file())): raise ValueError('异常文件类型: ' + str(q))
    return p

def under(path, root):
    p, r = canonical(path), canonical(root)
    if p == r or not p.is_relative_to(r): raise ValueError('路径超出允许范围: ' + str(p))
    return p

def run(command, env=None):
    result = subprocess.run([str(x) for x in command], capture_output=True, text=True, env=env)
    if result.returncode: raise RuntimeError(result.stderr.strip() or result.stdout.strip() or 'Command failed')
    return result.stdout.strip()

def mono_env(game):
    env = os.environ.copy(); env['MONO_PATH'] = str(game/'BepInEx/core')
    return env

def mono_path():
    p = shutil.which('mono')
    if not p:
        for q in ['/opt/homebrew/bin/mono','/usr/local/bin/mono','/Library/Frameworks/Mono.framework/Versions/Current/Commands/mono']:
            if Path(q).is_file(): p=q;break
    if not p: raise RuntimeError('需要 Mono 运行兼容检查器，请先安装 Mono。')
    return p

def inspect(game, dll):
    return json.loads(run([mono_path(), TOOL/'CompatPatcher.exe','--inspect',dll], mono_env(game)))

def patch(game, source, destination):
    text = run([mono_path(),TOOL/'CompatPatcher.exe',source,TOOL/'MacUpdateBridge.dll',destination,
                game/'Overcooked2.app/Contents/Resources/Data/Managed',game/'BepInEx/core'],mono_env(game))
    print(text, flush=True)
    return inspect(game, destination)

def validate_resources(root, expected_manifest=None):
    root = tree_check(root); manifest = root/'HostUtilities.Resources.manifest'
    if expected_manifest and sha(manifest) != expected_manifest: raise ValueError('资源清单哈希不符')
    listed = {manifest}
    for line in manifest.read_text(encoding='utf-8-sig').splitlines():
        m = re.fullmatch(r'([a-fA-F0-9]{64}) ([0-9]+) (.+)', line)
        if not m: raise ValueError('Invalid resource manifest')
        rel = m[3].replace('\\','/')
        if Path(rel).is_absolute(): raise ValueError('Absolute manifest path')
        p = under(root/rel, root)
        if p in listed: raise ValueError('Duplicate manifest path')
        if not p.is_file() or p.stat().st_size != int(m[2]) or sha(p) != m[1].lower(): raise ValueError('资源验证失败: '+rel)
        listed.add(p)
    if len(listed)<2: raise ValueError('Empty resource manifest')
    info = root/'HostUtilities.Resources.info.json'
    return (json.loads(info.read_text(encoding='utf-8-sig')) if info.is_file() else {}), listed

def fingerprint(root):
    tree_check(root)
    return {str(p.relative_to(root)):(sha(p) if p.is_file() else '<directory>') for p in sorted(Path(root).rglob('*')) if p.name!='.DS_Store'}

def curl(url, destination, binary=False):
    command=['curl','-fL','--retry','1','--connect-timeout','10','--max-time','90']
    if binary: command+=['-H','Accept: application/octet-stream']
    command += [url,'-o',str(destination)]
    run(command)

def download(tag, work):
    endpoint='latest' if tag=='latest' else 'tags/'+tag
    release=work/'release.json';curl(f'https://api.github.com/repos/{REPO}/releases/{endpoint}',release)
    data=json.loads(release.read_text())
    if data.get('draft') or data.get('prerelease'): raise ValueError('Only stable releases supported')
    assets=[a for a in data.get('assets',[]) if a['name']=='HostUtilities.zip']
    if len(assets)!=1: raise ValueError('找不到唯一的官方 HostUtilities.zip')
    asset=assets[0];digest=asset.get('digest','')
    if not digest.startswith('sha256:'): raise ValueError('GitHub 未提供 SHA-256，停止自动下载安装')
    archive=work/'HostUtilities.zip';curl(asset['url'],archive,True)
    if sha(archive)!=digest[7:]: raise ValueError('官方安装包哈希校验失败')
    return archive,data['tag_name'],digest[7:]

def unpack_dll(archive, destination):
    with zipfile.ZipFile(archive) as z:
        matches=[i for i in z.infolist() if Path(i.filename).name=='HostUtilities.dll' and not i.filename.startswith('__MACOSX/')]
        if len(matches)!=1: raise ValueError('安装包必须只有一个 HostUtilities.dll')
        i=matches[0]
        if i.file_size>40*1024*1024 or stat.S_ISLNK(i.external_attr>>16): raise ValueError('Invalid DLL archive entry')
        destination.write_bytes(z.read(i))

def config_component(game, host, work, plan=None):
    target=game/'BepInEx/plugins/ConfigurationManager/ConfigurationManager.dll';canonical(target)
    if target.exists():
        if inspect(game,target)['name']!='ConfigurationManager': raise ValueError('ConfigurationManager 目标属于其他组件')
        return None
    candidates=[]
    if plan and plan.get('ConfigurationManagerHash'):
        p=Path(plan['Directory'])/'ConfigurationManager.dll.stage'
        if sha(p)!=plan['ConfigurationManagerHash']: raise ValueError('配置面板暂存文件哈希不符')
        candidates=[p]
    else:
        candidates=[p for p in host.rglob('ConfigurationManager.dll') if p.is_file()]
    if not candidates: return None
    p=canonical(candidates[0])
    if inspect(game,p)['name']!='ConfigurationManager': raise ValueError('配置面板程序集不正确')
    out=work/'ConfigurationManager.dll';shutil.copy2(p,out)
    return {'source':str(out),'sha256':sha(out),'destination':str(target)}

def prepare(game, work, raw, expected_version=None, plan=None):
    game=canonical(game);host=game/'BepInEx/plugins/OC2HostUtilities';tree_check(host)
    if not host.is_dir(): raise ValueError('此工具用于更新已有 OC2HostUtilities，请先安装基础包')
    plan=plan or {};has_mod=raw is not None
    staged=work/'host.prepared';shutil.copytree(host,staged)
    info=None
    if has_mod:
        patched=work/'HostUtilities.mac.dll';info=patch(game,raw,patched)
        if info['name']!='HostUtilities': raise ValueError('不是 HostUtilities 程序集')
        v=version(info['version'])
        if expected_version and v!=version(expected_version): raise ValueError('版本号与下载信息不符')
        old_versions=[version(inspect(game,p)['version']) for p in host.rglob('HostUtilities.dll')]
        if old_versions and max(old_versions)>v: raise ValueError('拒绝覆盖较新的已安装版本')
        for p in [staged/'HostUtilities.dll',*[q/'HostUtilities.dll' for q in staged.iterdir() if q.is_dir() and re.fullmatch(r'v\d+\.\d+\.\d+(?:\.\d+)?',q.name)]]:
            if p.exists(): p.unlink()
        tag=(info.get('modInfo') or {}).get('tag') or 'v'+'.'.join(map(str,v[:3]))
        if version(tag)!=v: raise ValueError('程序集元数据版本不匹配')
        dest=staged/tag/'HostUtilities.dll';dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(patched,dest)
    else:
        existing=next((p for p in host.glob('v*/HostUtilities.dll')),host/'HostUtilities.dll')
        info=inspect(game,existing)
    if plan.get('HasResources'):
        resource_stage=Path(plan['Directory'])/'resources.stage'
        validate_resources(resource_stage,plan.get('ManifestHash'))
        shutil.rmtree(staged/'Resources',ignore_errors=False) if (staged/'Resources').exists() else None
        shutil.copytree(resource_stage,staged/'Resources')
    resource_info,listed=validate_resources(staged/'Resources')
    required=(info.get('modInfo') or {}).get('minResourceVersion')
    if required and (not resource_info.get('version') or version(resource_info['version'])<version(required)): raise ValueError('资源版本不足，保持原安装；请先安装满足要求的资源包')
    if resource_info.get('minModVersion') and version(info['version'])<version(resource_info['minModVersion']): raise ValueError('资源包要求更高版本的 MOD')
    component=config_component(game,host,work,plan)
    legacy=staged/'OC2HostUtilitiesResource'
    if legacy.exists():
        if component is None and (host/'OC2HostUtilitiesResource/ConfigurationManager/ConfigurationManager.dll').exists() and not (game/'BepInEx/plugins/ConfigurationManager/ConfigurationManager.dll').exists(): raise ValueError('配置面板尚未保存，停止旧资源迁移')
        # Only the prepared copy is removed. The entire live folder becomes a rollback backup.
        shutil.rmtree(legacy)
    recipe={'format':1,'game':str(game),'host':str(host),'staged':str(staged),'originalFingerprint':fingerprint(host),
            'preparedFingerprint':fingerprint(staged),'component':component,'version':info['version'],'officialOriginal':str(raw) if raw else None}
    path=work/'prepared.json';path.write_text(json.dumps(recipe,ensure_ascii=False,indent=2)+'\n')
    return path

def game_running():
    # Fail closed when process inspection is unavailable. Never install over a running game.
    output=run(['/bin/ps','-A','-o','comm='])
    return any(Path(line.strip()).name=='Overcooked2' for line in output.splitlines())

@contextmanager
def install_lock(game):
    path=canonical(game/'BepInEx/cache/OC2MacUpdater/installer.lock');path.parent.mkdir(parents=True,exist_ok=True)
    with path.open('a') as lock:
        try:fcntl.flock(lock,fcntl.LOCK_EX|fcntl.LOCK_NB)
        except BlockingIOError:raise RuntimeError('另一个 Mac 更新任务正在执行，请稍后重试')
        try:yield
        finally:fcntl.flock(lock,fcntl.LOCK_UN)

def apply_prepared(recipe_path, check_running=True):
    game=canonical(json.loads(Path(recipe_path).read_text())['game'])
    with install_lock(game):return _apply_prepared_locked(recipe_path,check_running)

def _apply_prepared_locked(recipe_path, check_running=True):
    recipe_path=canonical(recipe_path);recipe=json.loads(recipe_path.read_text());game=canonical(recipe['game']);host=game/'BepInEx/plugins/OC2HostUtilities'
    if canonical(recipe['host'])!=host: raise ValueError('Invalid installation target')
    staged=tree_check(recipe['staged'])
    if staged.parent!=recipe_path.parent or staged.name!='host.prepared': raise ValueError('Invalid staged tree')
    if fingerprint(host)!=recipe['originalFingerprint'] or fingerprint(staged)!=recipe['preparedFingerprint']: raise ValueError('准备后文件有变化，停止安装并保留原件')
    if check_running and game_running(): raise RuntimeError('请先完全退出 Overcooked! 2，再运行更新工具')
    backup=game/'BepInEx/backups/OC2MacUpdater'/(time.strftime('%Y%m%d-%H%M%S')+'-'+uuid.uuid4().hex[:8]);canonical(backup);backup.mkdir(parents=True)
    component=recipe['component'];created=None;swapped=False;original_moved=False
    try:
        if component:
            src=canonical(component['source']);dst=canonical(component['destination'])
            if dst!=game/'BepInEx/plugins/ConfigurationManager/ConfigurationManager.dll' or sha(src)!=component['sha256']: raise ValueError('Invalid component migration')
            if not dst.exists():
                dst.parent.mkdir(exist_ok=True);shutil.copy2(src,dst);created=dst
        os.rename(host,backup/'OC2HostUtilities');original_moved=True
        os.rename(staged,host);swapped=True
        report={'installedVersion':recipe['version'],'backup':str(backup),'componentMigrated':str(created) if created else None,'updatedAt':time.strftime('%Y-%m-%d %H:%M:%S')}
        (backup/'installation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
        if recipe.get('officialOriginal'):shutil.copy2(recipe['officialOriginal'],backup/'HostUtilities.official-original.dll')
    except BaseException:
        if swapped:os.rename(host,recipe_path.parent/'host.failed')
        if original_moved:os.rename(backup/'OC2HostUtilities',host)
        if created and created.exists():created.unlink()
        raise
    print('安装成功:',recipe['version'],'完整备份:',backup,flush=True)
    return backup

def validate_plan(plan):
    if plan.get('format')!=1: raise ValueError('Unsupported plan format')
    plugins=canonical(plan['PluginsRoot']);game=plugins.parent.parent
    if plugins!=game/'BepInEx/plugins' or canonical(plan['InstallRoot'])!=plugins/'OC2HostUtilities':raise ValueError('Invalid updater roots')
    directory=under(plan['Directory'],game/'BepInEx/cache/HostUtilitiesUpdates')
    if directory.parent!=game/'BepInEx/cache/HostUtilitiesUpdates' or not directory.name.startswith('plan-'):raise ValueError('Invalid updater cache directory')
    tree_check(directory);return game,directory

def cleanup_plan(game,directory,plan):
    host=game/'BepInEx/plugins/OC2HostUtilities';target=host/'Resources';_,listed=validate_resources(target)
    pairs=plan.get('ResourceBackups',[])
    for source,saved in pairs:
        src=canonical(source);saved=under(saved,directory)
        if src not in [host/'OC2HostUtilitiesResource',target] or saved.parent!=directory or not saved.name.startswith('resource-backup-'):raise ValueError('Invalid resource cleanup path')
        tree_check(src);tree_check(saved)
        if src==target:
            continue  # Manifest extras may contain user files; retain them, with backups.
        if not src.exists():continue
        for p in src.rglob('*'):
            if p.is_file() and p.name!='.DS_Store':
                q=saved/p.relative_to(src)
                if not q.is_file() or sha(p)!=sha(q):raise ValueError('旧资源有用户改动，保留目录: '+str(p))
        work=directory/('mac-cleanup-'+uuid.uuid4().hex[:8]);work.mkdir()
        component=config_component(game,host,work,plan)
        if component:
            dst=Path(component['destination']);dst.parent.mkdir(exist_ok=True);shutil.copy2(component['source'],dst)
        backup=game/'BepInEx/backups/OC2MacUpdater'/('resources-'+time.strftime('%Y%m%d-%H%M%S')+'-'+uuid.uuid4().hex[:8]);canonical(backup);backup.mkdir(parents=True)
        os.rename(src,backup/'OC2HostUtilitiesResource')
        print('旧资源已完整归档，包含 Finder 元数据:',backup,flush=True)

def wait_for_exit(pid):
    if pid<=1:raise ValueError('Invalid game process id')
    while True:
        try:os.kill(pid,0)
        except ProcessLookupError:return
        except PermissionError:raise RuntimeError('无法确认游戏是否退出，停止更新')
        time.sleep(1)

def worker(task,pid):
    task=canonical(task);plan=json.loads(task.read_text());game,directory=validate_plan(plan)
    if task.parent!=directory or task.name!='mac-install-plan.json':raise ValueError('Invalid task file')
    with (directory/'mac-apply.log').open('a',buffering=1) as log:
        sys.stdout=sys.stderr=log
        try:
            print('Mac worker ready; waiting for game exit',flush=True)
            (directory/'mac-worker-ready').write_text(str(os.getpid()))
            wait_for_exit(pid)
            # Revalidate after game exit; staged files may have changed while waiting.
            validate_plan(plan)
            if plan.get('cleanupOnly'):
                with install_lock(game):cleanup_plan(game,directory,plan)
            else:
                raw=directory/'HostUtilities.dll.stage' if plan.get('HasMod') else None
                if raw and sha(raw)!=plan['DllHash']:raise ValueError('下载 DLL 哈希发生变化')
                work=directory/('mac-work-'+uuid.uuid4().hex[:8]);work.mkdir()
                recipe=prepare(game,work,raw,plan.get('Version') if raw else None,plan)
                apply_prepared(recipe)
            (directory/'mac-apply-success').write_text(time.strftime('%Y-%m-%d %H:%M:%S'))
            if (directory/'restart.flag').exists():subprocess.Popen(['/usr/bin/open','steam://rungameid/728880'])
        except BaseException as e:
            print('Mac update stopped; originals retained:',repr(e),flush=True)
            (directory/'mac-apply-failed').write_text(str(e));raise

def main():
    a=argparse.ArgumentParser(description=__doc__)
    a.add_argument('--game-dir',type=Path,default=Path.home()/'Library/Application Support/Steam/steamapps/common/Overcooked! 2')
    a.add_argument('--version',default='latest');a.add_argument('--archive',type=Path);a.add_argument('--sha256');a.add_argument('--work-dir',type=Path)
    a.add_argument('--prepare-only',action='store_true');a.add_argument('--apply-prepared',type=Path);a.add_argument('--apply-plan',type=Path);a.add_argument('--wait-pid',type=int)
    args=a.parse_args()
    if args.apply_plan:
        if not args.wait_pid:raise ValueError('--wait-pid required')
        worker(args.apply_plan,args.wait_pid);return
    if args.apply_prepared:apply_prepared(args.apply_prepared);return
    game=canonical(args.game_dir)
    work=canonical(args.work_dir) if args.work_dir else game/'BepInEx/cache/OC2MacUpdater'/uuid.uuid4().hex
    work.mkdir(parents=True,exist_ok=False)
    if args.archive:
        if not args.sha256 or sha(args.archive)!=args.sha256.lower():raise ValueError('本地安装包必须提供正确的 --sha256')
        archive=args.archive;tag=None if args.version=='latest' else args.version
    else:archive,tag,_=download(args.version,work)
    raw=work/'HostUtilities.official.dll';unpack_dll(archive,raw)
    recipe=prepare(game,work,raw,tag)
    if args.prepare_only:print('准备完成，尚未修改游戏:',recipe)
    else:apply_prepared(recipe)

if __name__=='__main__':
    try:main()
    except Exception as e:print('更新停止:',e,file=sys.stderr);sys.exit(1)
