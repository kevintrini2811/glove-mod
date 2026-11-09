using LawnGlove;
using MelonLoader;
using Il2CppReloaded.Gameplay;
using UnityEngine;

[assembly: MelonInfo(typeof(Integration), "Lawn Glove integration tests", "1.1.2", "Tests")]
[assembly: MelonGame("PopCap Games", "PvZ Replanted")]
public class Integration : MelonMod
{
    private int step;
    private float started;
    private Plant plant;
    private void Check(bool ok, string label)
    {
        if (!ok) throw new Exception("FAIL: " + label);
        LoggerInstance.Msg("PASS: " + label);
    }
    private void Click(LawnGloveMod mod, Board b, int c, int r) =>
        mod.HandleBoardClick(b,b.GridToPixelX(c,r)+40,b.GridToPixelY(c,r)+40,1,0);

    public override void OnUpdate()
    {
        if (Environment.GetEnvironmentVariable("LAWN_GLOVE_TEST") != "1" || step < 0) return;
        var mod = LawnGloveMod.Instance;
        var b = mod?.Board;
        if (b == null || !mod.IsPlaying) return;
        try
        {
            if (step == 0)
            {
                if (started == 0) { started=Time.unscaledTime;return; }
                if (Time.unscaledTime-started<2) return;
                b.RemoveAllPlants();
                plant=b.AddPlant(1,1,SeedType.Peashooter,(SeedType)(-1));
                plant.mPlantHealth=147;
                var occupied=b.AddPlant(3,1,SeedType.Wallnut,(SeedType)(-1));
                Check(!plant.NotOnGround() && !occupied.NotOnGround(),"normal plants remain on ground");
                Check(b.GetTopPlantAt(1,1,PlantPriority.Any)?.Pointer==plant.Pointer,"native lookup finds planted plant");
                Check(b.CanPlantAt(1,1,SeedType.Peashooter)!=PlantingReason.Ok,"vanilla rejects planting on occupied tile");
                Check(!PlantMover.IsEmpty(b,1,1),"glove sees occupied tile");
                mod.Toggle();mod.Activate();
                Check(mod.State.Armed,"glove arms");
                Click(mod,b,1,1);
                Check(mod.IsCarrying(plant) && !plant.mVisible,"plant picked up and hidden at source");
                Check(plant.NotOnGround(),"only carried plant excluded by native ground rule");
                Check(!occupied.NotOnGround() && b.GetTopPlantAt(3,1,PlantPriority.Any)?.Pointer==occupied.Pointer,"other plants stay findable while carrying");
                Click(mod,b,3,1);
                Check(mod.IsCarrying(plant) && mod.State.Remaining==0,"occupied destination rejected and pickup retained");
                Click(mod,b,2,2);
                Check(plant.mPlantCol==2 && plant.mRow==2 && plant.mPlantHealth==147,"same plant placed across rows preserving health");
                Check(!mod.IsCarrying(plant) && plant.mVisible && !plant.NotOnGround(),"placed plant restored to native gameplay");
                Check(b.GetTopPlantAt(2,2,PlantPriority.Any)?.Pointer==plant.Pointer,"native lookup sees moved plant");
                Check(b.CanPlantAt(2,2,SeedType.Peashooter)!=PlantingReason.Ok,"moved plant prevents stacking");
                Check(!mod.State.Armed && mod.State.Remaining==10,"one move starts cooldown");
                Click(mod,b,4,2);Check(plant.mPlantCol==2,"extra click does not move again");
                while(mod.State.SelectedTool != LawnTool.Shovel) mod.Toggle();
                b.PickUpTool(ReloadedObjectType.Shovel);
                Check(!mod.State.GloveSelected && b.mCursorObject.CursorType==CursorType.Shovel,"shovel usable during cooldown");
                started=Time.unscaledTime;step=1;
            }
            else if(step==1 && Time.unscaledTime-started>2)
            {
                mod.Toggle();
                Check(mod.State.Remaining>0 && mod.State.Remaining<9,"cooldown advances with shovel selected");
                mod.Activate();
                Check(!mod.State.Armed,"glove cannot activate before expiry");step=2;
            }
            else if(step==2 && mod.State.Remaining==0)
            {
                Check(Time.unscaledTime-started>=9.8,"ten second real cooldown elapsed");
                mod.Activate();Click(mod,b,2,2);
                Check(mod.IsCarrying(plant),"can pick up after recharge");
                mod.Cancel();
                Check(!mod.IsCarrying(plant) && plant.mVisible && !plant.NotOnGround(),"cancel restores plant");
                Check(b.CanPlantAt(2,2,SeedType.Peashooter)!=PlantingReason.Ok,"cancelled plant still blocks its tile");
                Check(mod.State.Remaining==0,"cancel does not consume charge");
                mod.Activate();Click(mod,b,2,2);
                mod.BeforeSavingOrDisposing(b);
                Check(!mod.IsCarrying(plant) && plant.mVisible && !plant.NotOnGround(),"save boundary restores pickup");
                LoggerInstance.Msg("ALL ENGINE INTEGRATION TESTS PASSED; READY FOR REAL MOUSE TEST");
                step=-1;
            }
        }
        catch(Exception e){LoggerInstance.Error(e);step=-1;}
    }
}
