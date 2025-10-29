using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Compilation;
using UnityEngine;
using Utils;

namespace Game.Meta
{
    public class MetaAudioManager : Singleton<MetaAudioManager>
    {
        public AudioSource source;
        public TMP_Text theTextor;

        [System.Serializable]
        public struct Sound
        {
            public string name;
            public AudioClip clip;
        }
        
        public List<Sound> sounds = new();

        private void OnEnable()
        {
            CompilationPipeline.RequestScriptCompilation();
        }

        public void Play(string name)
        {
            print("Playing " + name);
            var s = sounds.Find(x => x.name == name);
            if (source.isPlaying)
            {
                source.Stop();
                theTextor.text = "";
            }
            
            StopAllCoroutines();
            StartCoroutine(nameof(TextClear));
            theTextor.text = name switch
            {
                "keysay" =>
                    "小玩需要按AD或左右键控制小帅移动，按下Q或X切换到上一个道具，按下E或V切换到下一个道具，按下空格、Shift、C进行跳跃，按下J或Z使用道具，请按R返回上次记录位置，长按R重置关卡",
                "meta 1-1.1" => "注意看，神秘男从天而降。从高空落在房顶上，这个高度普通人肯定九死一生。",
                "meta 1-1.2" => "但他明显不是一般人，走上天台就准备往楼对面跳。",
                "meta 1-1.3" => "注意看，他脚下一滑，就摔下楼。可惜这栋楼只有一层高。",
                "meta 1-2.1" => "注意看，这个男人叫小帅，虽然他不知道自己是怎么来到这里的，但是他应该很清楚怎么出去。",
                "meta 1-2.3" => "这种型号的按钮非常坚固，被子弹射击时依然可以工作。你知道有什么和这种按钮一样坚固吗？......此处广告位招租",
                "meta 1-2.4" => "小帅趁着电梯⻔打开的间隙冲了出去，摔到了电梯上。希望他不会被挤下去。",
                "meta 1-3.1.1" => "小玩按下了按钮，关卡并没有和人物一起被重置。这是计划的一部分。",
                "meta 1-3.1" => "震惊！小帅买饮料居然不花钱，看来它的数值真的很差！\n（玩家触碰初音色售货机后会在当前位置存档，轻推R键返回存档位置）",
                "meta 1-3.2" => "小帅请捂上耳朵，这一段不适合你听。短按 r 键可以重置。",
                "meta 1-3.3" => "小帅的小短腿好像不太能在⻔关闭之前赶到那里，他也许需要多使用一下他的超级智慧而不是超级力量。",
                "meta 1-3.4" => "冷知识！你知道吗，爆机不一定要靠游戏技术。用枪也可以做到\n（深蓝色售货机可以被子弹攻击，售货机受击之后会保存受击时玩家位置，当玩家轻推R键时返回保存位置）",
                "meta 1-3.5.1" => "注意看，小帅又一次踩到了陷阱，他现在正在考虑是否要换一个操作者",
                "meta 1-3.5" => "好消息，小玩。你踩到陷阱了，请⻓按 r 键重置地图。",
                "meta 1-4奖杯" => "注意看，这个男人触发了不知道隐藏在哪的机关。随着一阵特效，他按理应该被传送走......所以肖帅，麻烦你自己动起来坐上电梯去往下一个区域。",
                "meta 1-5土狼靴子" =>
                    "按照一般的设计，你⻅到的这种靴子使用之后会让你可以二段跳什么的。这个和那些也差不多，不过因为经费问题。毕竟请你们俩还挺贵的。总之这个靴子能让你把本来在地面上的跳跃机会留到空中用。不过你还是只能跳一次，将就用吧！",
                "meta 1-5.2" => "小帅应该意识到，仅凭着跳跃他没法违背物理学定律到达上面。他或许应该想想。如何以不可能的方式移动。",
                "meta 1-6.1" =>
                    "注意看，小帅。他还在管道深处不紧不慢，仿佛忘了这里叫‘第十三街’，而不是‘散步大道’。小玩，你最好让他想起来——而且是立刻、⻢上。管道里的氧气，或者别的什么东西，可不会等他。",
                "meta i-1机关" => "小帅不小心碰到了一个隐藏的机关，希望他没事。虽然看起来不太像没事的样子。",
                "meta i-1入场" => "谁把摄影棚的传送门连到后台来了？小玩——麻烦管好你的玩具，别让他碰那些加速板，我们还没有给他的加速上安全限制，出现的意外情况可不能算工伤。",
                "meta i-2入场" => "注意看，神秘男从天而降。等等，这不是小帅吗？从下面的场景来看，他还要这样坠落很多次。希望他做好了准备！",
                "meta i-3.2" =>
                    "看着这么高的墙，就算小玩你知道在道具后摇内切换道具能取消道具 cd 这种事情，也没有办法了。\n（使用道具后按下切换键切换回刚刚使用的道具可以触发取消后摇、CD的特性。致使某些道具的效果可以叠加）",
                "meta i-3.3" => "万万没想到，小帅居然把雨伞当成了降落伞！那么质量这么好的雨伞，应该去哪买呢。 我也不知道，但是危险操作请勿模仿。",
                "meta i-3.4" =>
                    "突然，小帅惊奇的发现，他在切枪之后再次开枪，子弹会自动分裂去命中自己的上一个目标。这难道就是传说中的枪斗术？\n（左轮在进入CD之前被切换会导致左轮击中机关的记录不被清除，导致击中墙体或别的机关连带着会触发以前记录下来的最多5个机关；当击中机关记录被清除时小玩可以听见一声轻的爆破声）",
                "meta i-3捡起降落伞" => "万万没想到，小帅居然把雨伞当成了降落伞！那么质量这么好的雨伞，应该去哪买呢。 我也不知道，但是危险操作请勿模仿。",
                "meta nobug" => "你没有开麦说过“这难道不是bug吗？”这句话吧？没有说过就好",
                "meta over" => "前方存在不详气息，我不会再陪你们前行了，路上保重。",
                "nomorepause" => "小玩发现不能再叠加新的暂停菜单了",
                "noshuzhi" => "小玩的数值疑似不是很高",
                "nostoppause" => "小玩现在还不想结束暂停。",
                "whodesigned" => "小玩在想，这是哪个天才设计的机关",
                "wrongbtn" => "小玩把音效按钮当成了出口",
                "finishchatting" => "小玩为什么听完了主创团队的碎碎念",
                "fall to stg 1" => "请为坠落的人类命名——开玩笑的，先使用降落伞减速一下别被摔死吧，如果你有的话，哈哈。",
                "fakebtn" => "小玩按下按钮但没有任何反应。",
                "entrystudio" => "小玩念出了神秘咒语，误闯到了妙妙屋。",
                _ => theTextor.text
            };

            if (s.clip)
                source.PlayOneShot(s.clip);
        }
        
        public IEnumerator TextClear()
        {
            yield return new WaitForSeconds(10f);
            theTextor.text = "";
        }
    }
}