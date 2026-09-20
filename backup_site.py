import os
import sys
import urllib.request
import urllib.parse
from bs4 import BeautifulSoup
import re

BASE_URL = "http://martinflegl.cz/"
BACKUP_DIR = r"C:\Users\janky\.gemini\antigravity\scratch\MartinFlegl\backup"

os.makedirs(BACKUP_DIR, exist_ok=True)

headers = {
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
}

def download_file(url, target_path):
    if os.path.exists(target_path) and os.path.getsize(target_path) > 0:
        return
    try:
        os.makedirs(os.path.dirname(target_path), exist_ok=True)
        req = urllib.request.Request(url, headers=headers)
        with urllib.request.urlopen(req, timeout=10) as resp:
            data = resp.read()
            with open(target_path, 'wb') as f:
                f.write(data)
        print(f"[OK] Downloaded: {url}")
    except Exception as e:
        print(f"[ERR] Failed {url}: {e}")

print("=== Starting backup of http://martinflegl.cz/ ===")

index_html_path = os.path.join(BACKUP_DIR, "index.html")
try:
    req = urllib.request.Request(BASE_URL, headers=headers)
    with urllib.request.urlopen(req, timeout=15) as resp:
        html_content = resp.read().decode('utf-8', errors='ignore')
        with open(index_html_path, 'w', encoding='utf-8') as f:
            f.write(html_content)
    print(f"[OK] Saved index.html ({len(html_content)} bytes)")
except Exception as e:
    print(f"[ERR] Failed to fetch index.html: {e}")
    sys.exit(1)

# Asset collection
soup = BeautifulSoup(html_content, 'html.parser')
asset_urls = set()

for tag, attr in [('img', 'src'), ('link', 'href'), ('script', 'src'), ('a', 'href')]:
    for el in soup.find_all(tag):
        val = el.get(attr)
        if val:
            full_u = urllib.parse.urljoin(BASE_URL, val)
            asset_urls.add(full_u)

# Regex matching for inline CSS/JS URLs
urls_in_text = re.findall(r'(https?://[^\s"\'\(\)\<\>]+|/[^\s"\'\(\)\<\>]+)', html_content)
for u in urls_in_text:
    if any(ext in u.lower() for ext in ['.css', '.js', '.jpg', '.jpeg', '.png', '.gif', '.svg', '.woff', '.woff2', '.ttf', '.eot', '.pdf', 'wp-content', 'wp-includes']):
        full_u = urllib.parse.urljoin(BASE_URL, u)
        asset_urls.add(full_u)

print(f"Found {len(asset_urls)} candidate assets...")

count = 0
for asset_url in sorted(asset_urls):
    parsed = urllib.parse.urlparse(asset_url)
    domain = parsed.netloc.replace(':', '_')
    path = parsed.path.lstrip('/')
    if not path or path.endswith('/'):
        continue
    
    clean_path = path.split('?')[0].split('#')[0]
    if not clean_path:
        continue
    
    # Store by domain or relative path
    if 'martinflegl.cz' in domain or not domain:
        target_path = os.path.join(BACKUP_DIR, os.path.normpath(clean_path))
    else:
        target_path = os.path.join(BACKUP_DIR, "external_assets", domain, os.path.normpath(clean_path))
    
    download_file(asset_url, target_path)
    count += 1

print(f"=== Backup completed! Processed {count} assets. ===")
