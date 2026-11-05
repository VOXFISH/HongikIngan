using System;
using UnityEngine;
using HongikIngan.Audio;

namespace HongikIngan
{
    public abstract class PrototypeScreen : MonoBehaviour
    {
        protected PrototypeRun Run => PrototypeSession.Ensure().Run;
        protected GUIStyle normal, heading, small, button, title;
        protected bool audioOpen;
        static Font font;
        protected readonly Color ink = new Color(.12f,.16f,.24f);
        protected readonly Color accent = new Color(.37f,.30f,.78f);
        protected void BeginUI(string screenTitle)
        {
            InitializeUI();
            Panel(new Rect(0,0,1440,900),new Color(.95f,.95f,.99f));
            Panel(new Rect(0,0,1440,100),new Color(.89f,.88f,.98f));
            Label(30,20,700,45,screenTitle,title);
            Label(32,66,1000,28,$"마법소녀전대  ·  STAGE {Run.Stage}  ·  미라클 에너지 {Run.Money}  ·  폴라로이드 {PhotoCount()}",small);
            if (Button(1260,25,150,46,"사운드 설정"))
            {
                if (audioOpen) SoundManager.EnsureExists().SaveSettings();
                audioOpen=!audioOpen;
            }
        }
        protected void InitializeUI()
        {
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width/1440f, Screen.height/900f,1));
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},20);
            normal = new GUIStyle(GUI.skin.label) { font=font, fontSize=18, wordWrap=true, normal={textColor=ink} };
            small = new GUIStyle(normal) { fontSize=14 };
            heading = new GUIStyle(normal) { fontSize=24, fontStyle=FontStyle.Bold };
            title = new GUIStyle(heading) { fontSize=30 };
            button = new GUIStyle(GUI.skin.button) { font=font,fontSize=17,wordWrap=true,
                normal={background=Texture2D.whiteTexture,textColor=ink},
                hover={background=Texture2D.whiteTexture,textColor=accent},
                active={background=Texture2D.whiteTexture,textColor=accent},
                onNormal={background=Texture2D.whiteTexture,textColor=accent},
                onHover={background=Texture2D.whiteTexture,textColor=accent},
                onActive={background=Texture2D.whiteTexture,textColor=accent} };
        }
        protected int PhotoCount() { int count=0; foreach(var n in Run.Photos.Values) count+=n; return count; }
        protected void Panel(Rect r,Color c) { var old=GUI.color; GUI.color=c; GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=old; }
        protected void Label(float x,float y,float w,float h,string text,GUIStyle style=null) => GUI.Label(new Rect(x,y,w,h),text,style??normal);
        protected bool Button(float x,float y,float w,float h,string text,bool enabled=true)
        {
            bool old=GUI.enabled; GUI.enabled=old&&enabled;
            bool hit=GUI.Button(new Rect(x,y,w,h),text,button); GUI.enabled=old; return hit;
        }
        protected virtual void Face(float x,float y,float size,StudentDefinition d)
        {
            int hash=0; foreach(char c in d.Id) hash=(hash*31+c)&0x7fffffff;
            Color c1=Color.HSVToRGB((hash%100)/100f,.28f,.96f);
            Panel(new Rect(x,y,size,size),c1);
            Label(x,y+size*.17f,size,size*.6f,"✦",new GUIStyle(heading){alignment=TextAnchor.MiddleCenter,fontSize=(int)(size*.45f)});
        }
        protected void StudentCard(Rect r,StudentDefinition d,string footer,bool selected=false)
        {
            Panel(r,selected?new Color(.79f,.76f,.97f):Color.white);
            if(r.width<180)
            {
                Face(r.x+12,r.y+12,35,d);
                Label(r.x+55,r.y+16,r.width-60,30,d.Name,small);
                Label(r.x+12,r.y+55,r.width-24,28,$"{d.Grade}학년 · {d.RoleLabel}",small);
                Label(r.x+12,r.y+88,r.width-24,48,$"{d.Club} · {d.Personality}",small);
                Label(r.x+12,r.y+140,r.width-24,48,footer,small);
                return;
            }
            Face(r.x+12,r.y+12,48,d);
            Label(r.x+70,r.y+8,r.width-75,30,d.Name,normal);
            Label(r.x+70,r.y+40,r.width-75,28,$"{d.Grade}학년 · {d.RoleLabel}",small);
            Label(r.x+12,r.y+76,r.width-24,48,$"{d.Club} · {d.Personality}",small);
            Label(r.x+12,r.y+125,r.width-24,48,footer,small);
        }
        protected void AudioUI()
        {
            if(!audioOpen) return;
            Panel(new Rect(450,190,540,470),new Color(.83f,.82f,.95f));
            Label(480,215,420,40,"사운드",heading);
            var manager=SoundManager.EnsureExists();
            var categories=new[]{AudioCategory.BGM,AudioCategory.SFX,AudioCategory.Voice};
            for(int i=0;i<categories.Length;i++)
            {
                var c=categories[i]; float y=285+i*95;
                Label(480,y,120,30,$"{c} {manager.GetVolume(c)*100:0}%");
                float v=GUI.HorizontalSlider(new Rect(610,y+10,220,25),manager.GetVolume(c),0,1);
                if(Math.Abs(v-manager.GetVolume(c))>.001f) manager.SetVolume(c,v);
                bool muted=GUI.Toggle(new Rect(850,y,115,35),manager.GetMuted(c),manager.GetMuted(c)?"음소거 켬":"음소거 끔",button);
                if(muted!=manager.GetMuted(c)) manager.SetMuted(c,muted);
            }
            Label(480,560,480,32,manager.Status,small);
            if(Button(790,595,160,40,"닫기"))
            {
                manager.SaveSettings();
                audioOpen=false;
            }
        }
    }
}
