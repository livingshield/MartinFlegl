import qrcode

vcard = """BEGIN:VCARD
VERSION:3.0
N:Flegl;Martin;;;
FN:Martin Flegl
TITLE:Nezávislý finanční specialista
TEL;TYPE=WORK,VOICE:+420736453532
EMAIL:martin.flegl@insia.com
URL:https://www.ekobio.org/MartinFlegl/
ADR;TYPE=WORK:;;Pražská 523;Trutnov;;541 01;Česká republika
END:VCARD"""

img = qrcode.make(vcard)
img.save("qr_kod.png")
