# FPKGi PS4 PKG Build Instructions

## المتطلبات الأساسية:

### 1. أدوات التطوير:
- **Orbis SDK** من Sony (متوفر للمطورين المسجلين)
- **Orbis Toolchain** (C++ Compiler و Linker)
- **orbis-pub-cmd** أو **ps4-pkg-tool** لإنشاء PKG
- **orbis_sfo** tools لإنشاء param.sfo
- **macpack** tool لإنشاء الملف النهائي (بديل)

### 2. البيانات المطلوبة:
```
project_root/
├── eboot.bin              # التطبيق الرئيسي المترجم
├── sce_module_info.yaml   # معلومات الوحدة النظام
├── sce_app_param.sfo      # معلومات التطبيق
├── icon0.png              # أيقونة التطبيق (256x256)
├── bg0.png                # صورة الخلفية
├── pic0.png               # صورة الـ Screenshot
└── assets/                # مجلد الموارد
```

---

## خطوات البناء:

### الخطوة 1: ترجمة المشروع
```bash
orbis-clang++ -o eboot.bin Assets/Scripts/*.cpp \
  -march=native -O3 -DNDEBUG -DORBIS \
  -Isdk/orbis/include \
  -Lsdk/orbis/lib -lorbis2d -lorbis_audio
```

### الخطوة 2: إنشاء ملف معلومات التطبيق
```bash
orbis_sfo.exe -u 0 -l 0 \
  -s TITLE "FPKGi v2.0.0" \
  -s TITLE_ID "FPKG00001" \
  -s VERSION "02.00" \
  -s APP_TYPE "Game" \
  -s CATEGORY "Game" \
  sce_app_param.sfo
```

### الخطوة 3: حزم الملفات
```bash
mkdir -p pkg/sce_sys
cp eboot.bin pkg/
cp icon0.png pkg/sce_sys/
cp bg0.png pkg/sce_sys/
cp pic0.png pkg/sce_sys/
cp sce_app_param.sfo pkg/sce_sys/
cp -r assets pkg/app/
```

### الخطوة 4: إنشاء الأرشيف النهائي أو PKG
```bash
python convert_to_pkg.py Builds/PS4
```

> ملاحظة: هذا السكريبت يحاول إنشاء `.pkg` إذا كانت أدوات مثل `orbis-pub-cmd.exe` أو `ps4-pkg-tool.exe` متوفرة.
> إذا لم تكن متوفرة، ينشئ أرشيف `build/FPKGi_v2.0.0_PS4.zip` بدلاً من ذلك.
> الأرشيف الناتج يحتوي على محتوى حزمة PS4 ولكنه ليس `.pkg` موقعًا نهائيًا.
> لإنشاء `.pkg` رسمي، تحتاج إلى Sony Orbis SDK وأدوات التوقيع المناسبة.

إذا كان لديك أدوات PS4 الرسمية، يمكنك بعدها بناء `.pkg` باستخدام أمر مشابه لما يلي:
```bash
orbis-pub-cmd.exe img_create --oformat pkg pkg/ FPKGi_v2.0.0_PS4.pkg
```

أو باستخدام macpack:
```bash
macpack.exe --type=pkg \
  --input=pkg \
  --output=FPKGi_v2.0.0_PS4.pkg \
  --key=ps4_key.bin \
  --custom_license="Freeware"
```

---

## البديل: استخدام DevKit

إذا كان لديك PS4 DevKit:

```bash
# نسخ الملفات إلى DevKit
orbis-lldb -- cp -r build_dir/ 192.168.1.x:/data/games/FPKG00001/

# تشغيل الملف مباشرة
orbis-debugger --attach=192.168.1.x:16666 eboot.bin
```

---

## معلومات إضافية:

### متطلبات التوقيع:
- **Private Key**: من Sony (للتطبيقات الرسمية فقط)
- **للـ Homebrew**: استخدم أدوات unsigned pkg tools

### الملفات الناقصة حالياً:
- ✗ `eboot.bin` - يجب ترجمة المشروع من C++
- ✗ صور الأيقونات - أضفها تحت `Assets/Images/`
- ✗ `sce_module_info.yaml` - أنشئ من النموذج

---

## ملاحظات قانونية:

⚠️ **تحذير مهم:**
- **لا تشارك** أدوات Sony Orbis SDK أو مفاتيح البيانات الخاصة
- استخدام DevKit يتطلب اتفاق قانوني مع Sony
- هذه التعليمات للأغراض التعليمية فقط

---

## الحل البديل:

إذا لم تتوفر أدوات Sony:
1. استخدم **مثبتات Homebrew** مثل `HEN` أو `Jailbreak Tools`
2. قم بتثبيت التطبيق مباشرة عبر USB
3. اعتمد على `.elf` بدلاً من `.pkg`
