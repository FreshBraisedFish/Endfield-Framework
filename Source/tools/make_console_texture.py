import sys
from PIL import Image, ImageDraw

S = 128
img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# 底座外壳
d.rounded_rectangle([6, 10, 122, 118], radius=10, fill=(38, 42, 48, 255), outline=(96, 104, 112, 255), width=3)
# 顶部散热条
for y in range(18, 30, 5):
    d.line([16, y, 112, y], fill=(70, 76, 84, 255), width=2)
# 屏幕
d.rounded_rectangle([18, 40, 110, 90], radius=6, fill=(10, 22, 26, 255), outline=(120, 200, 210, 200), width=2)
# 屏幕内容：终末地青
d.line([24, 50, 78, 50], fill=(90, 220, 220, 255), width=3)
d.line([24, 60, 96, 60], fill=(60, 170, 180, 255), width=2)
d.line([24, 68, 62, 68], fill=(60, 170, 180, 255), width=2)
d.line([24, 78, 88, 78], fill=(40, 130, 140, 255), width=2)
# 底部指示灯
d.ellipse([96, 100, 108, 112], fill=(255, 196, 64, 255))
d.ellipse([24, 100, 36, 112], fill=(120, 200, 210, 255))
# 支架
d.rectangle([20, 112, 108, 118], fill=(28, 31, 36, 255))

out = sys.argv[1]
img.save(out)
print("WROTE", out)