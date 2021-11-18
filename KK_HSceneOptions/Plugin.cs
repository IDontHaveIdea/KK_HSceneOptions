using System.Linq;
using BepInEx;

using static ChaFileDefine;
using UnityEngine;

namespace KK_HSceneOptions
{
    [BepInProcess("Koikatu")]
    [BepInProcess("KoikatuVR")]
    [BepInProcess("Koikatsu Party")]
    [BepInProcess("Koikatsu Party VR")]
    public partial class HSceneOptions : BaseUnityPlugin
    {
        public const string GUID = "MK.KK_HSceneOptions";
        public const string AssembName = "KK_HSceneOptions";

		private void Update()
		{
			if (!flags)
				return;

			if (Input.GetKeyDown(InsertWaitKey.Value.MainKey) && InsertWaitKey.Value.Modifiers.All(x => Input.GetKey(x)))
				OnInsertClick();
			else if (Input.GetKeyDown(InsertNowKey.Value.MainKey) && InsertNowKey.Value.Modifiers.All(x => Input.GetKey(x)))
				OnInsertNoVoiceClick();
			else if (Input.GetKeyDown(SwallowKey.Value.MainKey) && SwallowKey.Value.Modifiers.All(x => Input.GetKey(x)))
				flags.click = HFlag.ClickKind.drink;
			else if (Input.GetKeyDown(SpitKey.Value.MainKey) && SpitKey.Value.Modifiers.All(x => Input.GetKey(x)))
				flags.click = HFlag.ClickKind.vomit;

			if (Input.GetKeyDown(SubAccToggleKey.Value.MainKey) && SubAccToggleKey.Value.Modifiers.All(x => Input.GetKey(x)))
				ToggleMainGirlAccessories(category: 1);

			if (Input.GetKeyDown(PantsuStripKey.Value.MainKey) && PantsuStripKey.Value.Modifiers.All(x => Input.GetKey(x)))
				SetClothesStateRange(new ClothesKind[] { ClothesKind.shorts }, true);
			if (Input.GetKeyDown(TopClothesToggleKey.Value.MainKey) && TopClothesToggleKey.Value.Modifiers.All(x => Input.GetKey(x)))
				SetClothesStateRange(new ClothesKind[] { ClothesKind.top, ClothesKind.bra });
			if (Input.GetKeyDown(BottomClothesToggleKey.Value.MainKey) && BottomClothesToggleKey.Value.Modifiers.All(x => Input.GetKey(x)))
				SetClothesStateRange(new ClothesKind[] { ClothesKind.bot });
		}

	}
}