import extract_docx
import sys

text = extract_docx.extract_text_from_docx(r'C:\Users\lordk\Downloads\CV Jan Kytýr CZ.docx')
with open('cv.txt', 'w', encoding='utf-8') as f:
    f.write(text)
