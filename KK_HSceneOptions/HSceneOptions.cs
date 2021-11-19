//
// These are specific for Koikatu
//
using BepInEx;

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
	}
}
