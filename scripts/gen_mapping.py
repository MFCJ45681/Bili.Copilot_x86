#!/usr/bin/env python3
# gen_mapping.py - Convert a *.build.appxrecipe into a makeappx mapping file.
# Usage: python gen_mapping.py <path-to.build.appxrecipe> [output-mapping.txt]
# Reads every ItemGroup element that carries an Include attribute and a child
# <PackagePath> and emits a [Files] mapping for makeappx.exe pack.

import os
import sys
import xml.etree.ElementTree as ET


def main():
    if len(sys.argv) < 2:
        print("usage: gen_mapping.py <appxrecipe> [mapping.txt]")
        sys.exit(2)

    recipe = sys.argv[1]
    out = sys.argv[2] if len(sys.argv) > 2 else (os.path.splitext(recipe)[0] + ".mapping.txt")

    tree = ET.parse(recipe)
    root = tree.getroot()

    def find_pkgpath(el):
        # Appxrecipe may carry a default MSBuild namespace; match with wildcard.
        for tag in ("PackagePath", "{*}PackagePath"):
            p = el.find(tag)
            if p is not None and p.text:
                return p.text.strip()
        return None

    entries = []
    for el in root.iter():
        inc = el.get("Include")
        if not inc:
            continue
        pkg = find_pkgpath(el)
        if not pkg:
            continue
        entries.append((inc, pkg))

    # De-duplicate by package path, keep first occurrence.
    # Exclude leftover libVLC assets (reverted to mpv plugin).
    seen = set()
    unique = []
    for src, pkg in entries:
        if pkg in seen:
            continue
        if "libvlc" in pkg.lower() or "libvlc" in src.lower():
            continue
        seen.add(pkg)
        unique.append((src, pkg))

    with open(out, "w", encoding="utf-8") as f:
        f.write("[Files]\n")
        for src, pkg in unique:
            f.write(f'"{src}" "{pkg}"\n')

    # 补主 exe 条目。MSBuild 的 appxrecipe 不含 AOT 主程序，需自行追加。
    # 注意: 必须取"本次发布刚生成"的 AOT 产物。带 -p:Platform=x86 发布时产物在
    #   src/Desktop/BiliCopilot.UI/bin/x86/Release/<tfm>/<rid>/publish/
    # 而不带该参数(仅配置)时在 bin/Release/... —— 后者可能是历史陈旧文件，
    #   会导致包内 exe 与源码不一致(改动看似没生效)。故此处按 mtime 取最新。
    exe_entry = find_main_exe(recipe)
    if exe_entry:
        with open(out, "a", encoding="utf-8") as f:
            f.write(f'"{exe_entry}" "BiliCopilot.UI.exe"\n')
        print(f"main exe -> {exe_entry}")
    else:
        print("WARNING: main exe not found, mapping may be incomplete", file=sys.stderr)

    print(f"wrote {len(unique)} file entries -> {out}")


def find_main_exe(recipe):
    """定位与本次发布匹配的主程序 AOT 产物(取 mtime 最新者)."""
    # recipe 形如 <repo>/_build/x86/Release/BiliCopilot.UI/bin/xxx.appxrecipe
    # 主程序产物在 <repo>/src/Desktop/BiliCopilot.UI/bin/**/publish/BiliCopilot.UI.exe
    # 故从 recipe 所在目录逐级上溯，找到同时含 "src" 与 "scripts" 的仓库根。
    root = os.path.dirname(os.path.abspath(recipe))
    repo = None
    cur = root
    for _ in range(8):
        if os.path.isdir(os.path.join(cur, "src")) and os.path.isdir(os.path.join(cur, "scripts")):
            repo = cur
            break
        parent = os.path.dirname(cur)
        if parent == cur:
            break
        cur = parent

    if repo is None:
        return None

    bin_root = os.path.join(repo, "src", "Desktop", "BiliCopilot.UI", "bin")
    if not os.path.isdir(bin_root):
        return None

    candidates = []
    for dirpath, _dirnames, filenames in os.walk(bin_root):
        if os.path.basename(dirpath).lower() == "publish" and "BiliCopilot.UI.exe" in filenames:
            path = os.path.join(dirpath, "BiliCopilot.UI.exe")
            try:
                candidates.append((os.path.getmtime(path), path))
            except OSError:
                pass

    if not candidates:
        return None

    candidates.sort(reverse=True)
    return candidates[0][1].replace("\\", "/")


if __name__ == "__main__":
    main()
