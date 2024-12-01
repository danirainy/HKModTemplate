/*
 * 空洞骑士Mod入门到进阶指南/配套模版
 * 作者：近环（https://space.bilibili.com/1224243724）
 */

using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using Satchel;
using UnityEngine;

namespace HKModTemplate;

// Mod配置类，目前只有开关的配置。可以自行添加额外选项，并在GetMenuData里添加交互。
[Serializable]
public class Settings
{
    public bool on = true;
}

public class HKModTemplate : Mod, IGlobalSettings<Settings>, IMenuMod
{
    /* 
     * ******** Mod名字和版本号 ********
     */
    public HKModTemplate() : base("HKModTemplate")
    {
    }
    public override string GetVersion() => "1.0.0.1";


    /* 
     * ******** 预加载和hook ********
     */
    public override List<(string, string)> GetPreloadNames()
    {
        return [
            ("GG_Radiance", "Boss Control/Absolute Radiance")
        ];
    }
    // 光球模版
    private GameObject orbTemplate;
    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        // 添加需要使用的hooks
        On.PlayMakerFSM.OnEnable += PlayMakerFSM_OnEnable;

        var radiance = preloadedObjects["GG_Radiance"]["Boss Control/Absolute Radiance"];
        var fsm = radiance.LocateMyFSM("Attack Commands");
        orbTemplate = fsm.GetAction<SpawnObjectFromGlobalPool>("Spawn Fireball", 1).gameObject.Value;
        GameObject.Destroy(orbTemplate.GetComponent<PersistentBoolItem>());
        GameObject.Destroy(orbTemplate.GetComponent<ConstrainPosition>());
    }

    /* 
     * ******** FSM相关改动，这个示例改动使得左特随机在空中多次假动作 ********
     */
    private void PlayMakerFSM_OnEnable(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
    {
        // 玩家需要已经开启mod
        if (mySettings.on)
        {
            // 仅在当前场景是左特场景，对象是左特，且FSM的名字为Control时触发。关于如何查看FSM信息，请阅读教程。
            if (self.gameObject.scene.name == "GG_Grey_Prince_Zote" && self.gameObject.name == "Grey Prince" && self.FsmName == "Control")
            {
                void updateState(FsmState state)
                {
                    state.RemoveAction(3);
                    state.RemoveAction(7);
                    state.AddCustomAction(() =>
                    {
                        // 实例化光球
                        var orb = GameObject.Instantiate(orbTemplate);
                        // 位置
                        orb.transform.position = self.FsmVariables.GetFsmVector3("Spawn Pos").Value;
                        // 速度
                        var vX = self.FsmVariables.GetFsmFloat("X Vel").Value;
                        var vY = self.FsmVariables.GetFsmFloat("Y Vel").Value;
                        orb.GetComponent<Rigidbody2D>().velocity = new(vX, vY);
                        // 激活
                        orb.LocateMyFSM("Orb Control").SendEvent("FIRE");
                    });
                }
                updateState(self.GetState("Spit L"));
                updateState(self.GetState("Spit R"));
            }
        }
        // 如果不是完全重写该函数，只是增量改动，记得调用原函数。
        orig(self);
    }


    /* 
     * ******** 配置文件读取和菜单设置，如没有额外需求不需要改动 ********
     */
    private Settings mySettings = new();
    public bool ToggleButtonInsideMenu => true;
    // 读取配置文件
    public void OnLoadGlobal(Settings settings) => mySettings = settings;
    // 写入配置文件
    public Settings OnSaveGlobal() => mySettings;
    // 设置菜单格式
    public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? menu)
    {
        List<IMenuMod.MenuEntry> menus = new();
        menus.Add(
            new()
            {
                // 这是个单选菜单，这里提供开和关两种选择。
                Values = new string[]
                {
                    Language.Language.Get("MOH_ON", "MainMenu"),
                    Language.Language.Get("MOH_OFF", "MainMenu"),
                },
                // 把菜单的当前被选项更新到配置变量
                Saver = i => mySettings.on = i == 0,
                Loader = () => mySettings.on ? 0 : 1
            }
        );
        return menus;
    }
}
