using System.Collections;
using UnityEngine;

namespace HongikIngan
{
    public sealed class BattleScreen : PrototypeScreen
    {
        int drag=-1;
        bool running;
        float beam;
        string current="준비가 되면 합동 공격을 시작하세요.";
        void Start() { if(!Run.InBattle) Run.StartBattle(); var sound=HongikIngan.Audio.SoundManager.EnsureExists(); if(sound.BackendAvailable) sound.PlayBgm("event:/BGM/Battle"); }
        void OnGUI()
        {
            BeginUI("방과 후 · 합동 필살기");
            var battle=Run.Battle;
            if(battle==null) { if(Button(30,140,300,50,"학교로 돌아가기")) PrototypeSession.Instance.Restart(); return; }
            Label(35,120,650,50,$"TURN {battle.Turn}/3  ·  조합을 바꿔 보스를 물리치세요",heading);
            Label(35,180,800,75,$"합동 데미지  {FormatDamage(battle.Damage)}",title);
            Panel(new Rect(1040,170,350,230),new Color(.27f,.24f,.42f));
            Label(1070,205,280,70,"괴수",new GUIStyle(title){normal={textColor=Color.white},alignment=TextAnchor.MiddleCenter});
            Label(1070,300,290,55,$"HP {FormatDamage(battle.BossHp)} / {FormatDamage(battle.MaxBossHp)}",new GUIStyle(normal){normal={textColor=Color.white}});
            if(beam>0) Panel(new Rect(100,290,950,20+beam*25),new Color(.95f,.65f,.85f));
            for(int i=0;i<Run.Students.Count;i++)
            {
                var r=new Rect(30+i*153,425,143,195);
                StudentCard(r,Run.Students[i].Definition,$"스킬 {Run.Students[i].SkillValue:0.##}");
                if(!running&&!battle.Won&&!battle.Lost&&!audioOpen)
                {
                    if(Event.current.type==EventType.MouseDown&&r.Contains(Event.current.mousePosition)) { drag=i; Event.current.Use(); }
                    if(Event.current.type==EventType.MouseUp&&r.Contains(Event.current.mousePosition)&&drag>=0) { Run.Swap(drag,i); drag=-1; Event.current.Use(); }
                }
            }
            if(Event.current.type==EventType.MouseUp) drag=-1;
            Label(35,645,1360,45,current,heading);
            int start=Mathf.Max(0,battle.Log.Count-4);
            for(int i=start;i<battle.Log.Count;i++) Label(40,700+(i-start)*24,1050,25,battle.Log[i],small);
            if(battle.Won)
            { if(Button(1100,815,290,55,"승리! 교실로 복귀",!audioOpen&&!running)) PrototypeSession.Instance.BackToSchool(); }
            else if(battle.Lost)
            { if(Button(1100,815,290,55,"게임 오버 · 다시 시작",!audioOpen&&!running)) PrototypeSession.Instance.Restart(); }
            else if(Button(1100,815,290,55,running?"필살기 준비 중…":"합동 공격",!running&&!audioOpen)) StartCoroutine(Attack());
            AudioUI();
        }
        IEnumerator Attack()
        {
            running=true;
            foreach(var step in Run.ResolveTurn())
            {
                current=step.Message; beam=step.Kind=="beam"?1:0;
                var sound=HongikIngan.Audio.SoundManager.EnsureExists();
                if(sound.BackendAvailable && step.Kind=="beam") sound.PlayOneShot("event:/SFX/Beam",HongikIngan.Audio.AudioCategory.SFX);
                if(sound.BackendAvailable && step.Kind=="chant") sound.PlayOneShot("event:/Voice/Chant",HongikIngan.Audio.AudioCategory.Voice);
                yield return new WaitForSeconds(.35f);
                beam=0;
            }
            running=false;
        }
        public static string FormatDamage(float value)
        {
            if(float.IsNaN(value)) return "0";
            if(float.IsInfinity(value)) return "∞";
            double n=System.Math.Truncate(value);
            return System.Math.Abs(n)>=1e10 ? n.ToString("0.00e0",System.Globalization.CultureInfo.InvariantCulture) : n.ToString("N0",System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');
        }
    }
}
