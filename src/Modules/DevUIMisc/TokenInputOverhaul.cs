using DevInterface;
using MoreSlugcats;
using RegionKit.Modules.DevUIMisc.GenericNodes;

namespace RegionKit.Modules.DevUIMisc
{
	internal static class TokenInputOverhaul
	{
		internal static void Apply()
		{
			On.DevInterface.TokenRepresentation.TokenControlPanel.ctor += TokenControlPanel_ctor;
		}

		internal static void Undo()
		{
			On.DevInterface.TokenRepresentation.TokenControlPanel.ctor -= TokenControlPanel_ctor;
		}

		private static void TokenControlPanel_ctor(On.DevInterface.TokenRepresentation.TokenControlPanel.orig_ctor orig, TokenRepresentation.TokenControlPanel self, DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos)
		{
			orig(self, owner, IDstring, parentNode, pos);

			TokenRepresentation.TokenControlPanel.IndexControlSlider? slider = self.subNodes.FirstOrDefaultOfType<TokenRepresentation.TokenControlPanel.IndexControlSlider>();
			CollectToken.CollectTokenData TokenData = self.TokenData;
			List<string>? alternatives = null;
			if (TokenData.isWhite)
			{
				alternatives = ExtEnum<ChatlogData.ChatlogID>.values.entries;
			}
			else if (TokenData.isGreen)
			{
				alternatives = ExtEnum<MultiplayerUnlocks.SlugcatUnlockID>.values.entries;
			}
			else if (TokenData.isRed)
			{
				alternatives = ExtEnum<MultiplayerUnlocks.SafariUnlockID>.values.entries;
			}
			else if (TokenData.isBlue)
			{
				alternatives = ExtEnum<MultiplayerUnlocks.SandboxUnlockID>.values.entries;
			}
			else // there's no good way to check for gold :(
			{
				alternatives = ExtEnum<MultiplayerUnlocks.LevelUnlockID>.values.entries;
			}

			// Check that (1) we have a valid alternatives list and (2) the length matches what the token actually uses (which it might not for modded ones!)
			if (alternatives != null && slider != null && slider.maxNubInt == alternatives.Count - 1)
			{
				self.size.y += 20f;
				(self.subNodes[0] as PositionedDevUINode)!.pos.y += 20f;
				(self.subNodes[1] as PositionedDevUINode)!.pos.y += 20f;
				self.subNodes.Add(new TokenSelector(owner, IDstring, self, new Vector2(5f, (self.subNodes[1] as PositionedDevUINode)!.pos.y - 20f), 240f, "", alternatives)
				{
					Text = "Select token"
				});
			}
		}

		private class TokenSelector : ButtonWithSearchableSelectPanel
		{
			private TokenRepresentation.TokenControlPanel Panel => (parentNode as TokenRepresentation.TokenControlPanel)!;
			private TokenRepresentation Rep => (parentNode.parentNode as TokenRepresentation)!;
			private CollectToken.CollectTokenData TokenData => (Rep.pObj.data as CollectToken.CollectTokenData)!;

			public TokenSelector(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string defaultValue, List<string> values) 
				: base(owner, IDstring, parentNode, pos, width, defaultValue, values.ToArray(), "Select Token")
			{
			}

			public override void OnValueChange(string value)
			{
				//base.OnValueChange(value);
				if (ModManager.MSC && TokenData.isWhite)
				{
					TokenData.ChatlogCollect = new ChatlogData.ChatlogID(value);
				}
				else if (TokenData.isGreen)
				{
					TokenData.SlugcatUnlock = new MultiplayerUnlocks.SlugcatUnlockID(value);
				}
				else if (ModManager.MSC && TokenData.isRed)
				{
					TokenData.SafariUnlock = new MultiplayerUnlocks.SafariUnlockID(value);
				}
				else if (TokenData.isBlue)
				{
					TokenData.SandboxUnlock = new MultiplayerUnlocks.SandboxUnlockID(value);
				}
				else if (!TokenData.isBlue)
				{
					TokenData.LevelUnlock = new MultiplayerUnlocks.LevelUnlockID(value);
				}
				Panel.UpdateTokenText();
				Rep.Refresh();
			}
		}
	}
}
