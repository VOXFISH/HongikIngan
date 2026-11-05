using UnityEngine;

namespace HongikIngan
{
    public sealed class SchoolScreen : PrototypeScreen
    {
        int selected=-1, offer=-1, drag=-1;
        bool replacement, recruitReplacement, confirmRestart;
        void Start() { PrototypeSession.Ensure(); var sound=HongikIngan.Audio.SoundManager.EnsureExists(); if(sound.BackendAvailable) sound.PlayBgm("event:/BGM/School"); }
        void OnGUI()
        {
            bool wasEnabled=GUI.enabled;
            GUI.enabled=wasEnabled&&!recruitReplacement&&!confirmRestart;
            BeginUI("방과 전 · 우리들의 교실");
            GUI.enabled=wasEnabled;
            if(Button(1080,25,165,46,"새 게임",!Run.PendingPet&&!audioOpen&&!recruitReplacement&&!confirmRestart))
            { confirmRestart=true; drag=-1; }
            bool free=!Run.PendingPet&&!audioOpen&&!recruitReplacement&&!confirmRestart;
            Label(30,115,800,35,$"학생 {Run.Students.Count}/{Run.Capacity} · 책 = 보유 시너지 · 드래그해서 순서 교환",heading);
            for(int i=0;i<Run.Capacity;i++)
            {
                var r=new Rect(30+i*153,160,143,155);
                if(i<Run.Students.Count)
                {
                    var d=Run.Students[i].Definition;
                    Panel(r,selected==i?new Color(.78f,.75f,.95f):Color.white);
                    Face(r.x+12,r.y+10,40,d);
                    Label(r.x+10,r.y+56,133,30,d.Name,small);
                    Label(r.x+10,r.y+88,130,55,$"{d.Grade}학년 {d.Role}\n책 {new string('▣', d.SynergyCount)}",small);
                    if(free)
                    {
                        if(Event.current.type==EventType.MouseDown&&r.Contains(Event.current.mousePosition)) { drag=i; selected=i; Event.current.Use(); }
                        if(Event.current.type==EventType.MouseUp&&r.Contains(Event.current.mousePosition)&&drag>=0)
                        { if(drag!=i&&Run.Swap(drag,i)) selected=i; drag=-1; Event.current.Use(); }
                    }
                }
                else { Panel(r,new Color(.89f,.90f,.94f)); Label(r.x+20,r.y+60,110,35,"빈 책상",small); }
            }
            if(Event.current.type==EventType.MouseUp) drag=-1;
            if(Button(30,330,180,42,$"책상 확장 · {Run.Settings.slotExpansionPrice}",free)) Run.ExpandSlots();
            if(selected>=0&&selected<Run.Students.Count)
            {
                var student=Run.Students[selected];
                Label(230,324,750,65,$"{student.Definition.Name} · {student.Definition.Description}\n{student.Definition.SkillDescription}  현재 값 {student.SkillValue:0.##} / 강화 {student.UpgradeCount}",small);
                if(Button(1000,330,190,42,$"학습 · {Run.Settings.UpgradePrice(student.Definition.Grade)}",free)) Run.Upgrade(selected);
                if(Button(1205,330,195,42,$"계약 해제 +{Run.Settings.SalePrice(student.Definition.Grade)}",free)) { Run.Sell(selected); selected=-1; }
            }
            Label(30,400,440,35,"전학생 모집",heading);
            for(int i=0;i<Run.RecruitOffers.Count;i++)
            {
                var d=Run.RecruitOffers[i]; var r=new Rect(30+i*152,447,142,190);
                if(d==null) { Panel(r,Color.white); Label(r.x+15,r.y+65,120,35,"영입 완료",small); continue; }
                StudentCard(r,d,$"영입 {Run.Settings.RecruitPrice(d.Grade)} 에너지",offer==i);
                if(Button(r.x,r.y+193,142,38,"학생 정보",free)) offer=i;
            }
            if(offer>=0&&offer<Run.RecruitOffers.Count&&Run.RecruitOffers[offer]!=null)
            {
                var d=Run.RecruitOffers[offer];
                Label(30,688,440,65,$"{d.Name}: {d.SkillDescription}\n{d.Club} · {d.Personality}",small);
                if(Run.Students.Count>=Run.Capacity)
                {
                    if(Button(30,754,435,40,"교체 · 기존 학생 판매",free)) { recruitReplacement=true; drag=-1; }
                }
                else if(Button(30,754,435,40,"영입하기",free)&&Run.BuyRecruit(offer)) offer=-1;
            }
            Label(510,400,425,35,"유물 상점 · 획득 유물은 칠판에",heading);
            for(int i=0;i<Run.RelicOffers.Count;i++)
            {
                var relic=Run.RelicOffers[i];
                if(Button(510,447+i*64,420,55,relic==null?"구매 완료":$"{relic.Name} · {Run.Settings.relicPrice} 에너지\n{relic.Description}",free&&relic!=null)) Run.BuyRelic(i);
            }
            string owned="칠판  "; foreach(var relic in Run.Relics) owned+=relic.Name+" / ";
            Label(510,650,420,135,owned,small);
            Label(975,400,430,35,"펫 부르기 · 비용에 영입 포함",heading);
            for(int i=0;i<Run.PetOffers.Count;i++)
            {
                var kind=Run.PetOffers[i];
                string desc=kind.HasValue?PetLabel((int)kind.Value):"이용 완료";
                if(Button(975,447+i*64,430,55,desc,free&&kind.HasValue)) { Run.OpenPet(i); replacement=false; }
            }
            Label(975,652,420,110,"선결제 → 후보 확인 → 최대 1명 영입\n남은 후보는 폴라로이드로!\n포기 시 환불 없음",small);
            if(Button(30,825,300,48,$"전체 리롤 · {Run.RerollCost}",free)) { Run.Reroll(); offer=-1; }
            Label(355,823,690,65,Run.LastMessage,small);
            if(Button(1115,825,290,48,"방과 후 → 전투",free&&Run.Students.Count>0)) PrototypeSession.Instance.EnterBattle();
            if(Run.PendingPet) PetSelection();
            if(recruitReplacement) RecruitReplacement();
            if(confirmRestart) RestartConfirmation();
            AudioUI();
        }
        string PetLabel(int kind)
        {
            string[] labels={"일반 펫","고급 펫","최고급 펫","시너지 펫"};
            return labels[Mathf.Clamp(kind,0,3)]+$" · {Run.Settings.PetPrice((PetKind)kind)} / 후보 {(kind==1||kind==2?5:3)}명";
        }
        void PetSelection()
        {
            Panel(new Rect(70,180,1300,610),new Color(.86f,.84f,.97f));
            Label(100,205,1150,45,"새로운 인연 · 1명 선택 / 남은 후보는 폴라로이드",heading);
            for(int i=0;i<Run.PendingCandidates.Count;i++)
            {
                var d=Run.PendingCandidates[i]; var r=new Rect(105+i*245,275,230,240);
                StudentCard(r,d,d.SkillDescription);
                if(Button(r.x,r.y+245,230,44,"이 학생 영입",Run.Students.Count<Run.Capacity&&!audioOpen)) { Run.ChoosePet(i); replacement=false; break; }
            }
            if(Button(105,605,260,45,"교체 · 기존 학생 판매",!audioOpen)) replacement=!replacement;
            if(Button(1040,700,270,45,"영입하지 않음 (환불 없음)",!audioOpen)) { Run.SkipPet(); replacement=false; }
            Label(105,708,900,45,Run.LastMessage,small);
            if(replacement)
            {
                Panel(new Rect(385,580,920,115),new Color(.95f,.93f,.99f));
                ReplacementStudents(395,590);
            }
        }
        bool ReplacementStudents(float x,float y)
        {
            for(int i=0;i<Run.Students.Count;i++)
            {
                var d=Run.Students[i].Definition; float left=x+i*100;
                // Portrait and name share one click target; no combat details in this panel.
                if(Button(left,y,95,92,"",!audioOpen))
                {
                    bool sold=Run.Sell(i);
                    if(sold) selected=-1;
                    return sold;
                }
                Face(left+30,y+5,35,d);
                Label(left+4,y+43,87,45,d.Name,small);
            }
            return false;
        }
        void RecruitReplacement()
        {
            Panel(new Rect(0,100,1440,800),new Color(.12f,.16f,.24f,.6f));
            Panel(new Rect(220,290,1000,305),new Color(.95f,.93f,.99f));
            Label(245,310,935,40,"교체 · 학생의 얼굴이나 이름을 누르면 즉시 계약 해제합니다",heading);
            Label(245,355,920,35,"판매 금액을 받고 빈자리를 만든 뒤 선택한 전학생을 영입할 수 있습니다.",small);
            if(ReplacementStudents(255,410)) recruitReplacement=false;
            if(Button(1010,530,175,42,"닫기")) recruitReplacement=false;
        }
        void RestartConfirmation()
        {
            Panel(new Rect(0,100,1440,800),new Color(.12f,.16f,.24f,.6f));
            Panel(new Rect(420,310,600,260),new Color(.95f,.93f,.99f));
            Label(450,335,540,45,"새 게임을 시작할까요?",heading);
            Label(450,390,540,65,"현재 학생, 재화, 유물, 폴라로이드와 진행 상황이 초기화됩니다.",normal);
            if(Button(450,490,245,45,"취소")) confirmRestart=false;
            if(Button(720,490,270,45,"초기화하고 새 게임"))
            {
                PrototypeSession.Instance.Restart();
                GUIUtility.ExitGUI();
            }
        }
    }
}
