// Boss03Idle20.cs - chay Idle cua BOSS (nu quy canh tim) 20 frame LAP VO HAN, khong can Animator.
// Dung cho sheet boss03_idle_20f_transparent_1280x1024.png:
//   1280x1024, luoi 5 cot x 4 hang, o 256x256, 20 frame, khong co o trong.
//   Nhan vat dung thang, canh tim, tho nhe + canh phay nhe. Boss03 = cung nhan vat nu quy canh tim nhu boss demon, ban JPG goc 1280x1024, da tach trong suot. Khong chop mat ro
//   nhu hero (khong co frame nham rieng), nen nhip de deu.
// KHAC HeroIdle: hero chi co 4-5-12 frame, boss Idle nay co 20 frame.
//
// Cach dung:
//  1. Keo file PNG trong suot vao Assets, keo file nay vao Assets/Scripts.
//  2. Slice: Sprite Mode Multiple, Grid By Cell Count Column 5, Row 4,
//     Pivot Bottom (Bottom Center), Slice, Apply.
//     Unity danh so tu DUOI len, nen so Unity KHONG trung voi thu tu nhin:
//       Nhin trai sang phai, tren xuong duoi:
//         hang tren:    _15 _16 _17 _18 _19
//         hang 2:       _10 _11 _12 _13 _14
//         hang 3:       _5  _6  _7  _8  _9
//         hang duoi:    _0  _1  _2  _3  _4
//     Vi vay KHONG keo _0.._19 theo so, ma keo theo thu tu nhin tren:
//     _15,_16,_17,_18,_19,_10,_11,_12,_13,_14,_5,_6,_7,_8,_9,_0,_1,_2,_3,_4
//  3. Tao object tu sprite frame dau (_15), doi ten Boss,
//     Add Component Boss03Idle20.
//  4. Frames Size = 20, keo 20 sprite theo dung thu tu tren.
//  5. Play On Start va Loop de tick, bam Play la chay lap vo han.
//
// Nhịp mặc định: moi frame 0,125 giay (8 fps), tong vong 2,50 giay.
// Chi sua so trong Inspector, khong can sua code.
//
// Kiem tra khi chay (da do tu sheet goc):
//  - Chan deu o cung baseline: chan cach day o 14px o ca 4 hang,
//    dinh dau 23-24px, cao nhan vat 218-219px, khong nhay len xuong.
//  - Khong tran frame: rong nhan vat 193-203px trong o 256px,
//    le trai 5-11px, le phai 48-52px, khong dinh frame ke ben.
//  - Nhan vat lech trai khoang 20px so voi tam o, nhung lech DEU o ca 20 frame,
//    nen khi chay khong giat ngang. Pivot Bottom Center se giu chan co dinh.
//  - Nhan vat dung thang, mat nhin thang, khong can Flip X cho Idle.
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Boss03Idle20 : MonoBehaviour
{
    [Tooltip("Dung thu tu nhin trai-phai, tren-duoi: _15.._19, _10.._14, _5.._9, _0.._4. Khong keo _0.._19 theo so.")]
    public Sprite[] frames;

    [Tooltip("Thoi gian giu tung frame (giay), phai khop so luong voi frames.")]
    public float[] frameDurations = { 0.125f, 0.125f, 0.125f, 0.125f, 0.125f, 0.125f, 0.125f, 0.125f, 0.125f, 0.125f, 0.125f, 0.125f, 0.125f, 0.125f, 0.125f, 0.125f, 0.125f, 0.125f, 0.125f, 0.125f };

    public bool playOnStart = true;
    public bool loop = true;

    private SpriteRenderer sr;
    private float timer;
    private int index;
    private bool playing;

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
        playing = true;
        index = 0;
        timer = 0f;
        sr.sprite = frames[0];
    }

    public void Stop()
    {
        playing = false;
    }

    float DurationFor(int i)
    {
        if (frameDurations != null && i < frameDurations.Length && frameDurations[i] > 0f)
            return frameDurations[i];
        return 0.125f;
    }

    void Update()
    {
        if (!playing || frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        while (timer >= DurationFor(index))
        {
            timer -= DurationFor(index);
            index++;
            if (index >= frames.Length)
            {
                if (loop)
                {
                    index = 0;
                }
                else
                {
                    playing = false;
                    index = frames.Length - 1;
                    sr.sprite = frames[index];
                    return;
                }
            }
            sr.sprite = frames[index];
        }
    }
}
