// DragonFireballAttack13.cs - chay Attack lua tim cua rong 13 frame MOT LAN, khong lap.
// Dung cho sheet 13 frame trong suot 1600x1280, luoi 5 cot x 4 hang, o 320x320,
// 13 frame, 7 o trong.
// Doc trai sang phai, tren xuong duoi, BO QUA o trong:
//   Hang 1 (frame 0-4): tich lua, mieng mo dan, cau lua lon dan
//   Hang 2 (frame 5-6): phun lua, luong lua chay sang phai
//     (3 frame phun lua o giua da bi xoa, nen lua se nhay tu gan sang xa)
//   Hang 3 (frame 7-10): frame 7 lua tan, cau lua xa, frame 8-10 ve dung, khong lua
//     (frame o cot 2 hang 3 da bi xoa)
//   Hang 4 (frame 11-12): ve dung yen
// KHAC ban 17 frame: ban nay chi con 13 frame, tong thoi gian ngan hon,
//   lua bay se giat hon vi thieu frame trung gian.
//
// Cach dung:
//  1. Keo file PNG trong suot 1600x1280 vao Assets, keo file nay vao Assets/Scripts.
//  2. Slice: Sprite Mode Multiple, Grid By Cell Count Column 5, Row 4,
//     Pivot Bottom (Bottom Center), Slice, Apply.
//     Grid 5x4 se tao ra 20 o, trong do 7 o trong.
//     Unity danh so tu duoi len, nen so Unity KHONG trung voi thu tu nhin.
//     Thu tu keo Frames, theo dang rong, bo qua o trong:
//       frame 0 = _15, frame 1 = _16, frame 2 = _17, frame 3 = _18, frame 4 = _19,
//       frame 5 = _10, frame 6 = _11,
//       frame 7 = _5,  frame 8 = _7,  frame 9 = _8,  frame 10 = _9,
//       frame 11 = _0, frame 12 = _1
//       o trong _2,_3,_4,_6,_12,_13,_14 bo qua, khong keo vao Frames.
//     Vi vay KHONG keo theo so _0.._19, ma keo theo dang rong.
//     Tot nhat la sau khi Slice thi xoa 7 o trong truoc khi Apply.
//  3. Tao object tu sprite frame 0, doi ten DragonBoss,
//     Add Component DragonFireballAttack13.
//  4. Frames Size = 13, keo 13 sprite theo dung thu tu tren.
//  5. Play On Start tick neu muon bam Play la danh 1 lan ngay.
//     Bo tick neu muon goi tu code: GetComponent<DragonFireballAttack13>().Play();
//  6. Sau khi chay xong, sprite dung o frame cuoi, IsPlaying = false,
//     code tran co the bat lai script Idle cua rong.
//
// Nhịp mặc định: tong ~1,26 giay
//   tich lua 0-4: 0,10 moi frame
//   phun lua 5-6: 0,10, 0,08
//   lua tan / ve dung 7-12: 0,10, 0,08, 0,08, 0,08, 0,12, 0,12
//   Cu the: 0,10 0,10 0,10 0,10 0,10  0,10 0,08  0,10 0,08 0,08 0,08  0,12 0,12
// Muon danh nhanh hon thi giam deu, vi du moi frame 0,08 giay thi tong ~1,04 giay.
// Chi sua so trong Inspector, khong can sua code.
//
// Luu y huong danh: rong trong sheet danh sang PHAI.
// Neu rong dung ben PHAI tran, danh ve phia hero ben trai,
// thi phai lat ngang: tren SpriteRenderer tick Flip X = true.
// Khong lat thi rong se phun lua ra ngoai man hinh, nguoc huong hero.
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DragonFireballAttack13 : MonoBehaviour
{
    [Tooltip("Dung thu tu: 0-4 tich lua, 5-6 phun lua, 7 lua tan, 8-12 ve dung. Bo qua o trong.")]
    public Sprite[] frames;

    [Tooltip("Thoi gian giu tung frame (giay), phai khop so luong voi frames.")]
    public float[] frameDurations = { 0.10f, 0.10f, 0.10f, 0.10f, 0.10f};

    [Tooltip("Tick de bam Play la danh 1 lan ngay. Bo tick de goi Play() tu code / logic tran.")]
    public bool playOnStart = true;

    [Tooltip("Tick de sau khi danh xong tu quay ve frame dau tien. Bo tick de dung o frame cuoi.")]
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
        return 0.10f;
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
