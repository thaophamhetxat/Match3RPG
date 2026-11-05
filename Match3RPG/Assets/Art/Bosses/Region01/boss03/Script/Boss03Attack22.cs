// Boss03Attack22.cs - chay Attack cua BOSS (nu quy canh tim) 22 frame MOT LAN, khong lap, khong can Animator.
// Dung cho sheet boss03_attack_22f_transparent_2560x2560.png:
//   2560x2560, luoi 5 cot x 5 hang, o 512x512 (chia het, khong can sua lai nhu ban 2048 cu), 22 frame co nhan vat, 3 o trong.
//   Frame 0-14: danh (chong gay len, dam gay sang phai, chem vong tim, impact, thu gay).
//   Frame 15-21: dung yen tho nhe sau khi danh xong (7 frame, gan giong Idle).
// KHAC BossAttack12 (sheet cu 1280x768): sheet cu chi co 12 frame, sheet nay co 22 frame. Boss03 = cung nhan vat nhu boss demon, ban JPG goc 2560x2560, o 512 chia het.
// KHAC Idle: Idle lap vo han. Attack chi chay 1 lan roi dung o frame cuoi (dung yen).
//
// Cach dung:
//  1. Keo file PNG trong suot 2050x2050 vao Assets, keo file nay vao Assets/Scripts.
//  2. Slice: Sprite Mode Multiple, Grid By Cell Count Column 5, Row 5,
//     Pivot Bottom (Bottom Center), Slice.
//     Grid 5x5 se tao ra 25 o, trong do 3 o cuoi hang duoi la o trong.
//     XOA 3 o trong truoc khi Apply, neu khong se keo nham o trong vao Frames.
//     Unity danh so tu DUOI len, nen so Unity KHONG trung voi thu tu nhin:
//       Nhin trai sang phai, tren xuong duoi:
//         hang tren:    _20 _21 _22 _23 _24
//         hang 2:       _15 _16 _17 _18 _19
//         hang 3:       _10 _11 _12 _13 _14
//         hang 4:       _5  _6  _7  _8  _9
//         hang duoi:    _0  _1  (_2 _3 _4 la o trong, bo qua)
//     Vi vay KHONG keo _0.._24 theo so, ma keo theo thu tu nhin tren:
//     _20,_21,_22,_23,_24,_15,_16,_17,_18,_19,_10,_11,_12,_13,_14,_5,_6,_7,_8,_9,_0,_1
//  3. Tao object tu sprite frame 0 (_20), doi ten Boss,
//     Add Component Boss03Attack22.
//  4. Frames Size = 22, keo 22 sprite theo dung thu tu tren.
//  5. Play On Start tick neu muon bam Play la danh 1 lan ngay.
//     Bo tick neu muon goi tu code: GetComponent<Boss03Attack22>().Play();
//  6. Sau khi chay xong, sprite dung o frame cuoi (_1, dung yen),
//     IsPlaying = false, code tran co the bat lai script BossIdle20.
//
// Nhịp mặc định: tong ~2,17 giay, frame impact giu lau nhat:
//   _0 dung: 0,12 | _1-5 dam gay: 0,08 0,08 0,07 0,07 0,07
//   _6 chem vong: 0,08 | _7 IMPACT (chem ngang rong nhat): 0,16
//   _8-9 thu gay: 0,07 0,07 | _10-11: 0,08 0,08 | _12-14 ve dung: 0,10 0,10 0,10
//   _15-21 dung yen sau danh: 0,12 moi frame
// Muon danh nhanh hon thi giam deu, nhung frame impact nen giu lau hon
// cac frame khac de thay ro hieu ung chem.
// Chi sua so trong Inspector, khong can sua code.
//
// Luu y HUONG DANH va CO NHAN VAT - doc ky truoc khi chay:
//  - Sheet nay boss dam/chem sang PHAI. Boss cua ban dung ben PHAI tran,
//    danh ve phia hero ben trai, thi phai lat ngang:
//    tren SpriteRenderer tick Flip X = true, hoac Scale X = -1.
//    Khong lat thi boss se danh ra ngoai man hinh, nguoc huong hero.
//  - CO NHAN VAT KHAC IDLE: frame dung cua Attack cao chi 183px
//    trong o 512px, trong khi Idle cao 220-222px trong o 256px.
//    Neu de cung Scale voi Idle (cung Pixels Per Unit), boss se NHO LAI
//    ~1,21 lan khi danh (221/183), roi to lai khi ve Idle. De khop chieu
//    cao, dat Scale cua object Attack gap ~1,21 lan Scale cua object Idle
//    (kiem tra bang mat trong Play: dau boss cao ngang nhau o 2 sheet,
//    chan cung baseline). Ban 2560 nay tot hon ban 2048 cu (o 512 chia het,
//    khong can sua thanh 2050, ti le lech chi 1,21 thay vi 1,5).
//    Chan cua Attack cach day o 40-41px o ca 5 hang, khong nhay len xuong,
//    nen chi can chinh Scale, khong can chinh vi tri chan.
//  - Frame impact _7 (nhin la frame thu 8): hieu ung chem ngang rong nhat,
//    rong hon nhan vat, nen o 512px la can thiet, dung thu nho o.
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Boss03Attack22 : MonoBehaviour
{
    [Tooltip("Dung thu tu nhin trai-phai, tren-duoi: _20.._24, _15.._19, _10.._14, _5.._9, _0,_1. Bo 3 o trong _2,_3,_4.")]
    public Sprite[] frames;

    [Tooltip("Thoi gian giu tung frame (giay), phai khop so luong voi frames. Frame impact giu lau hon.")]
    public float[] frameDurations = { 0.12f, 0.08f, 0.08f, 0.07f, 0.07f, 0.07f, 0.08f, 0.16f, 0.07f, 0.07f, 0.08f, 0.08f, 0.10f, 0.10f, 0.10f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f };

    [Tooltip("Tick de bam Play la danh 1 lan ngay. Bo tick de goi Play() tu code / logic tran.")]
    public bool playOnStart = true;

    [Tooltip("Tick de sau khi danh xong tu quay ve frame dau tien. Bo tick de dung o frame cuoi (dung yen).")]
    public bool returnToFirstFrameOnFinish = false;

    public bool IsPlaying { get; private set; }

    private SpriteRenderer sr;
    private float timer;
    private int index;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        if (frames == null || frames.Length == 0) return;
        IsPlaying = true;
        index = 0;
        timer = 0f;
        sr.sprite = frames[0];
    }

    float DurationFor(int i)
    {
        if (frameDurations != null && i < frameDurations.Length && frameDurations[i] > 0f)
            return frameDurations[i];
        return 0.08f;
    }

    void Update()
    {
        if (!IsPlaying || frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        while (timer >= DurationFor(index))
        {
            timer -= DurationFor(index);
            index++;
            if (index >= frames.Length)
            {
                IsPlaying = false;
                if (returnToFirstFrameOnFinish)
                {
                    index = 0;
                    sr.sprite = frames[0];
                }
                else
                {
                    sr.sprite = frames[frames.Length - 1];
                }
                return;
            }
            sr.sprite = frames[index];
        }
    }
}
