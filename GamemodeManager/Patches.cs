using Harmony;

namespace GamemodeManager
{
	class LaterJoinPatch
	{
		[HarmonyPatch(typeof(CharacterClassManager), "LaterJoinPossible")]
		public static void Postfix(bool __result)
		{
			__result = __result == true ? Configs.isLaterJoin : false;
		}
	}
}
