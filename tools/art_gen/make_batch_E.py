"""make_batch_E.py - Bộ phông chữ tiếng Việt cho Game2D: LÔ E (Phông chữ tiếng Việt cho giao diện).

Tuân thủ nghiêm ngặt:
- Art Contract (docs/ref/14A_ART_CONTRACT_PHU_LUC.md mục 12.3)
- Phông chữ mã nguồn mở giấy phép OFL: Be Vietnam Pro (The Be Vietnam Pro Project Authors)
- Bộ ký tự: Hỗ trợ 100% tiếng Việt đầy đủ dấu (ă, â, đ, ê, ô, ơ, ư, dấu thanh hỏi, ngã, sắc, huyền, nặng)
- Thử nghiệm bắt buộc: "Quyền truy cập đã niêm phong. Hạ Giữ Neo, tuyến kế tiếp mở. Ặ Ễ Ỡ Ữ"
- Đầy đủ 3 trọng số: Regular (văn bản thường), SemiBold (nút & nhãn HUD), Bold (tiêu đề & số sát thương)
"""
import os
import shutil
import urllib.request
from PIL import Image, ImageDraw, ImageFont

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
OUT_DIR = os.path.join(BASE_DIR, 'out_ui_v2')
os.makedirs(OUT_DIR, exist_ok=True)

ART_FONTS_DIR = os.path.join(BASE_DIR, '..', '..', 'Assets', '_Game', 'Art', 'UI', 'Fonts')
RES_FONTS_DIR = os.path.join(BASE_DIR, '..', '..', 'Assets', '_Game', 'Resources', 'UISkin', 'Fonts')
os.makedirs(ART_FONTS_DIR, exist_ok=True)
os.makedirs(RES_FONTS_DIR, exist_ok=True)

GITHUB_FONT_BASE = "https://raw.githubusercontent.com/google/fonts/main/ofl/bevietnampro"

FONTS_TO_FETCH = [
    ("BeVietnamPro-Regular.ttf",  f"{GITHUB_FONT_BASE}/BeVietnamPro-Regular.ttf"),
    ("BeVietnamPro-SemiBold.ttf", f"{GITHUB_FONT_BASE}/BeVietnamPro-SemiBold.ttf"),
    ("BeVietnamPro-Bold.ttf",     f"{GITHUB_FONT_BASE}/BeVietnamPro-Bold.ttf"),
    ("OFL.txt",                  f"{GITHUB_FONT_BASE}/OFL.txt")
]

TEST_SENTENCE = "Quyền truy cập đã niêm phong. Hạ Giữ Neo, tuyến kế tiếp mở. Ặ Ễ Ỡ Ữ"

def download_and_verify():
    print("=== TẢI VÀ KIỂM TRA PHÔNG CHỮ TIẾNG VIỆT LÔ E (BE VIETNAM PRO - OFL) ===")
    verified_fonts = {}
    
    for filename, url in FONTS_TO_FETCH:
        local_temp = os.path.join(BASE_DIR, filename)
        if not os.path.exists(local_temp) or os.path.getsize(local_temp) < 1000:
            print(f"  [DOWNLOAD] Đang tải {filename}...")
            urllib.request.urlretrieve(url, local_temp)
        
        size = os.path.getsize(local_temp)
        print(f"  [OK] {filename} ({size:,} bytes)")
        
        # Copy to Art/UI/Fonts/ and Resources/UISkin/Fonts/
        art_dest = os.path.join(ART_FONTS_DIR, filename)
        res_dest = os.path.join(RES_FONTS_DIR, filename)
        shutil.copy2(local_temp, art_dest)
        shutil.copy2(local_temp, res_dest)
        
        if filename.endswith('.ttf'):
            verified_fonts[filename] = local_temp

    return verified_fonts

def generate_font_previews(verified_fonts):
    print("\n--- Đang tạo ảnh xem thử nghiệm phông chữ Lô E ---")
    
    # 1. Preview so sánh 3 trọng số
    pw, ph = 1280, 320
    im = Image.new('RGBA', (pw, ph), (11, 17, 20, 255)) # Steel dark background
    draw = ImageDraw.Draw(im)
    
    # Header
    title_font = ImageFont.truetype(verified_fonts["BeVietnamPro-Bold.ttf"], 28)
    draw.text((30, 24), "THỬ NGHIỆM PHÔNG CHỮ TIẾNG VIỆT LÔ E — BE VIETNAM PRO (OFL 1.1)", fill=(239, 212, 122, 255), font=title_font)
    
    weights = [
        ("Regular (Văn bản / Nhật ký)",  verified_fonts["BeVietnamPro-Regular.ttf"],  (194, 201, 212, 255)),
        ("SemiBold (HUD / Nút bấm)",    verified_fonts["BeVietnamPro-SemiBold.ttf"], (244, 247, 251, 255)),
        ("Bold (Tiêu đề / Số sát thương)", verified_fonts["BeVietnamPro-Bold.ttf"],     (255, 242, 176, 255)),
    ]
    
    y = 80
    for label, path, col in weights:
        lbl_font = ImageFont.truetype(verified_fonts["BeVietnamPro-Regular.ttf"], 18)
        content_font = ImageFont.truetype(path, 24)
        draw.text((30, y), label, fill=(140, 148, 161, 255), font=lbl_font)
        draw.text((30, y + 26), TEST_SENTENCE, fill=col, font=content_font)
        y += 75
        
    preview_weights_path = os.path.join(OUT_DIR, 'preview_E_all_weights.png')
    im.save(preview_weights_path)
    print(f"  [PREVIEW] Đã lưu bảng so sánh 3 trọng số: {preview_weights_path}")
    
    # 2. Preview mẫu giao diện UI thực chiến
    mw, mh = 1280, 540
    mock = Image.new('RGBA', (mw, mh), (16, 22, 29, 255))
    mdraw = ImageDraw.Draw(mock)
    
    # Header
    mdraw.text((40, 30), "MẪU HIỂN THỊ GIAO DIỆN GAME VỚI PHÔNG BE VIETNAM PRO", fill=(239, 212, 122, 255), font=title_font)
    
    # Box 1: Thông số HUD chiến đấu
    mdraw.rectangle([40, 90, 480, 240], fill=(23, 38, 42, 240), outline=(93, 129, 135, 255), width=2)
    h_font = ImageFont.truetype(verified_fonts["BeVietnamPro-Bold.ttf"], 22)
    b_font = ImageFont.truetype(verified_fonts["BeVietnamPro-SemiBold.ttf"], 18)
    mdraw.text((60, 105), "THÔNG SỐ THỦY THỦ", fill=(239, 212, 122, 255), font=h_font)
    mdraw.text((60, 145), "Cấp độ: Lv.15 (Nhà Thám Hiểm Sóng)", fill=(244, 247, 251, 255), font=b_font)
    mdraw.text((60, 175), "Máu: 920 / 920 (100%)", fill=(134, 176, 196, 255), font=b_font)
    mdraw.text((60, 205), "Vị trí: Tầng 1 — Cảng Biển Luminport", fill=(205, 178, 124, 255), font=b_font)
    
    # Box 2: Thông báo chiến đấu / Chu kỳ ngày đêm
    mdraw.rectangle([520, 90, 960, 240], fill=(23, 38, 42, 240), outline=(93, 129, 135, 255), width=2)
    mdraw.text((540, 105), "CHU KỲ THẾ GIỚI & NHẬT KÝ", fill=(239, 212, 122, 255), font=h_font)
    mdraw.text((540, 145), "Thời gian: Ngày 2 · 06:45 · Sáng sớm", fill=(244, 247, 251, 255), font=b_font)
    mdraw.text((540, 175), "Kỹ năng: Nhát Đâm Mũi Neo kích hoạt!", fill=(247, 214, 94, 255), font=b_font)
    mdraw.text((540, 205), "Cảnh báo: Thủy triều đang dâng ở cổng vòm", fill=(223, 149, 112, 255), font=b_font)

    # Box 3: Màn hình Niêm Phong Tử Thần (Death Screen)
    mdraw.rectangle([40, 270, 960, 480], fill=(38, 26, 8, 240), outline=(199, 160, 58, 255), width=2)
    death_title = ImageFont.truetype(verified_fonts["BeVietnamPro-Bold.ttf"], 32)
    death_body = ImageFont.truetype(verified_fonts["BeVietnamPro-Regular.ttf"], 20)
    death_btn = ImageFont.truetype(verified_fonts["BeVietnamPro-SemiBold.ttf"], 22)
    
    mdraw.text((60, 290), "BẠN ĐÃ BỊ NIÊM PHONG", fill=(223, 149, 112, 255), font=death_title)
    mdraw.text((60, 345), "Hành trình tại Tháp Thủy Triều đã dừng lại vĩnh viễn.", fill=(244, 247, 251, 255), font=death_body)
    mdraw.text((60, 375), "Ổ khóa cổ và dấu sáp đỏ đã phong ấn toàn bộ trang bị cùng dữ liệu nhân vật.", fill=(205, 178, 124, 255), font=death_body)
    
    # Nút bấm mẫu
    mdraw.rectangle([60, 415, 320, 465], fill=(59, 44, 25, 255), outline=(239, 212, 122, 255), width=2)
    mdraw.text((85, 425), "Tạo Thủy Thủ Mới", fill=(244, 247, 251, 255), font=death_btn)

    mdraw.rectangle([340, 415, 520, 465], fill=(37, 57, 61, 255), outline=(93, 129, 135, 255), width=2)
    mdraw.text((395, 425), "Rời Tháp", fill=(194, 201, 212, 255), font=death_btn)

    preview_mockup_path = os.path.join(OUT_DIR, 'preview_E_ui_mockup.png')
    mock.save(preview_mockup_path)
    print(f"  [PREVIEW] Đã lưu mẫu hiển thị giao diện UI: {preview_mockup_path}")

def main():
    fonts = download_and_verify()
    generate_font_previews(fonts)
    print("\n>>> HOÀN THÀNH TẤT CẢ TỆP VÀ XEM THỬ CHO LÔ E!")

if __name__ == '__main__':
    main()
