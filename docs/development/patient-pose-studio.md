# Hasta poz stüdyosu (Blender) — elle poz verme kılavuzu

Amaç: hastanın yatış pozunu Blender'da **elle** oturtmak, tek komutla bake edip
Unity'ye almak. Fizik/ragdoll yok; poz deterministik olarak prefab'a bake edilir,
nefes efekti (`PatientVisualController`) üstünde çalışmaya devam eder.

## 1. Stüdyoyu üret (bir kez, ya da model değişince)

```bash
QANIVA_POSE_STUDIO="$PWD/art/patient-pose-studio.blend" \
/Applications/Blender.app/Contents/MacOS/Blender -b --python scripts/build-patient-from-hp.py -- \
  "<Hospital Patient zip'inin açıldığı klasör>" /dev/null /tmp/x 28 78 -8
```

Dosya `art/patient-pose-studio.blend` (git'e girmez). İçinde:
- **Armature** — 20 kemik: `Hips, Spine, Chest, UpperChest, Neck, Head, Shoulder/UpperArm/LowerArm/Hand.L|R, Thigh/Calf/Foot.L|R`
- dokulu **Body** + önlük, gözler, kirpik, ağız, bileklik (hepsi rig'e bağlı)
- **Bed_Mattress / Bed_Backrest / Bed_RailL / Bed_RailR** tel kafes yatak yüzeyleri (Unity ölçüleriyle; şilte üstü z = 0)
- Hasta sırtüstü, baş +Y'de, ayaklar −Y'de; başlangıç pozu bizim mevcut pozumuz (bake edilmemiş, oynayabilirsin)

## 2. Poz ver

1. `art/patient-pose-studio.blend` dosyasını Blender'da aç.
2. Armature'ı seç → **Ctrl+Tab** (Pose Mode). Kemiği seç, **R** ile döndür (R X / R Y / R Z eksen kilidi), **G** ile kaydırma yalnızca `Hips` için.
3. Hedef: kollar/eller şiltenin **üstünde** (z ≥ 0), gövde ve baş sırtlığa yaslı, dirsekler hafif bükük, ayaklar gevşek.
4. **Ctrl+S** ile kaydet. (Kemik adlarını değiştirme — Unity kontratı.)

Kolaylıklar: `N` panel → Item → kemik rotasyonlarını sayısal gir · `Alt+R` seçili kemiğin rotasyonunu sıfırlar · yatak wire kutuları `Object Mode`'da gizlenebilir.

## 3. Bake + Unity'ye export

```bash
/Applications/Blender.app/Contents/MacOS/Blender -b art/patient-pose-studio.blend \
  --python scripts/export-posed-patient.py -- unity/QanivaSimulation/Assets/Qaniva/Art/Patients/adult_hp_v1.fbx
```

Script önce **penetrasyon raporu** basar (`WARN Hand.L tail is 14 cm below the mattress top` gibi) — sıfır uyarıya kadar pozu düzelt. Sonra pozu rest'e bake eder, rig'i Unity'nin beklediği ayakta çerçeveye döndürür ve FBX'i yazar.

Ardından prefab + önizleme:

```bash
UNITY=$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity | sort -V | tail -1)
"$UNITY" -batchmode -quit -projectPath unity/QanivaSimulation -executeMethod Qaniva.EditorTools.QanivaPresentationAssets.CreateAll -logFile /tmp/assets.log
"$UNITY" -batchmode -quit -projectPath unity/QanivaSimulation -executeMethod Qaniva.EditorTools.QanivaPresentationAssets.CapturePreview -previewOut /tmp/preview.png -logFile /tmp/preview.log
```

Önizleme geometri açısından doğrudur (dokular headless'ta bozuk görünür); gerçek görüntü için simülatöre al: `SIM=1 scripts/export-unity-ios.sh` → `pod install` → build.

## Notlar

- Kaynak modelin lisansı sahibine aittir (`Art/Patients/LICENSE-hospital-patient.txt`); zip repo'ya girmez.
- Nefes: `Chest` kemiği solunum sayısıyla hareket eder; pozu bake etmek bunu bozmaz.
- Vaka bazlı varyant (ör. kadın hasta) için aynı stüdyo başka bir OBJ setiyle üretilir.
