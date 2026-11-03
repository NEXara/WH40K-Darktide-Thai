# เตรียมส่วนประกอบสำหรับชุด GitHub

ตามคำแนะนำของ deluxghost ตัวติดตั้งเผยแพร่บน GitHub ส่วน Nexus (หากมีภายหลัง) ลงเฉพาะคำแปลพร้อม dependencies ไปยังหน้าต้นฉบับ

## แหล่งทางการ

- [deluxghost/darktide-mods](https://github.com/deluxghost/darktide-mods) → Actions → Build → เลือกรันที่สำเร็จ → Artifacts ของ SimpleAssets และ WhatTheLocalization
- [Darktide Mod Loader](https://github.com/Darktide-Mod-Framework/Darktide-Mod-Loader)
- [Darktide Mod Framework](https://github.com/Darktide-Mod-Framework/Darktide-Mod-Framework)
- [dtkit-patch](https://github.com/manshanko/dtkit-patch)

Artifacts อาจต้องล็อกอิน GitHub และมีวันหมดอายุ ถ้าไม่พบแพ็กของม็อดนั้น ให้ดาวน์โหลดจากหน้า Nexus ของเจ้าของ ไม่เดาลิงก์และไม่ดึงไฟล์จากแหล่งอื่นแทนเงียบๆ

## ขั้นตอนก่อนเปลี่ยนรุ่นที่บรรจุ

1. ดาวน์โหลด artifact จากรันที่สำเร็จในคลังทางการไปโฟลเดอร์เตรียมงาน แยกจากแพ็กที่แจกอยู่
2. บันทึกชื่อ artifact, run ID, commit SHA, รุ่นม็อด, วันดาวน์โหลด และ SHA256
3. ตรวจไฟล์ native runtime และเอกสาร license ของม็อดนั้นด้วย ไม่คัดลอกเฉพาะ Lua
4. เทียบ API และการโหลดฟอนต์กับโค้ดแปลไทย ทดสอบตัวติดตั้ง หมวดคำแปล การคืนอังกฤษ และการทำงานในเกม
5. อัปเดต manifest/hash แล้วออกรุ่นใหม่ หากไม่ผ่านให้คงชุดที่ทดสอบเดิม อย่าอ้างว่าอัปเดตเป็นล่าสุดแล้ว

ชุด 1.0.3 ที่แจกครั้งนี้ใช้ส่วนประกอบเดิมที่ผ่านทดสอบ โดย SimpleAssets ระบุ 2.0.0 ไม่ได้สลับเป็นเวอร์ชันใหม่อัตโนมัติ การตรวจต้นทางวันที่ 4 พฤศจิกายน 2569 พบ WhatTheLocalization.zip ในรัน 41569747712 แต่ไม่พบ SimpleAssets.zip ที่ยังดาวน์โหลดได้จากการค้นหาชื่อ artifact ตรงตัว จึงไม่เปลี่ยน dependency ระหว่างเตรียม README

ตัว manifest ใน ZIP ระบุไฟล์ที่ใช้จริงและ hash ไว้ครบ การคอมไพล์ตัวติดตั้งใช้ source/BUILD.cmd บน Windows หลังปรับ payload ใช้ source/refresh_package.py อัปเดต hash และตรวจทั้งหมดก่อนแพ็กใหม่
