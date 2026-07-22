import qrcode

# vCard data for HonzaGG
vcard_data = """BEGIN:VCARD
VERSION:3.0
N:Kytýr;Jan;;Ing.;
FN:Ing. Jan Kytýr (HonzaGG)
ORG:HonzaGG
TITLE:Nezávislý finanční specialista
TEL;TYPE=WORK,VOICE:+420603196220
EMAIL;TYPE=WORK:jan.kytyr@seznam.cz
ADR;TYPE=WORK:;;Trutnov;Trutnov;;;Česká republika
URL:https://www.ekobio.org/HonzaGG/
END:VCARD"""

# Generate QR code
qr = qrcode.QRCode(version=1, box_size=10, border=4)
qr.add_data(vcard_data)
qr.make(fit=True)

img = qr.make_image(fill_color="black", back_color="white")
img.save(r"c:\Users\lordk\.gemini\antigravity\scratch\MartinFlegl\HonzaGG.Web\wwwroot\qr_kod.png")
