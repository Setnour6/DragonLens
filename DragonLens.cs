global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
global using Terraria;
global using Terraria.ModLoader;
using DragonLens.Configs;
using DragonLens.Content.Tools;
using DragonLens.Content.Tools.Spawners;
using DragonLens.Core.Loaders.UILoading;
using DragonLens.Core.Systems;
using DragonLens.Core.Systems.ToolSystem;
using ReLogic.Content;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Terraria.GameContent;
using Terraria.ID;

namespace DragonLens
{
	public class DragonLens : Mod
	{
		public override void PostAddRecipes()/* tModPorter Note: Removed. Use ModSystem.PostAddRecipes */
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			if (ModContent.GetInstance<ToolConfig>().preloadSpawners)
			{
				UILoader.GetUIState<ItemBrowser>().Refresh();
				UILoader.GetUIState<ItemBrowser>().initialized = true;

				UILoader.GetUIState<ProjectileBrowser>().Refresh();
				UILoader.GetUIState<ProjectileBrowser>().initialized = true;

				UILoader.GetUIState<NPCBrowser>().Refresh();
				UILoader.GetUIState<NPCBrowser>().initialized = true;

				UILoader.GetUIState<BuffBrowser>().Refresh();
				UILoader.GetUIState<BuffBrowser>().initialized = true;

				UILoader.GetUIState<TileBrowser>().Refresh();
				UILoader.GetUIState<TileBrowser>().initialized = true;

				UILoader.GetUIState<ToolBrowser>().Refresh();
				UILoader.GetUIState<ToolBrowser>().initialized = true;
			}

			if (ModContent.GetInstance<ToolConfig>().preloadAssets)
			{
				var itemThread = new Thread(() =>
				{
					Stopwatch watch = new();
					watch.Start();

					for (int k = 0; k < ItemID.Count; k++)
					{
						try
						{
							Main.Assets.Request<Texture2D>(TextureAssets.Item[k].Name, AssetRequestMode.AsyncLoad).Wait();
						}
						catch
						{
							Logger.Warn($"Item asset {k} failed to load");
							continue;
						}
					}

					watch.Stop();
					Logger.Info($"Item assets finished loading in {watch.ElapsedMilliseconds} ms");
				})
				{
					IsBackground = true
				};
				itemThread.Start();

				var projThread = new Thread(() =>
				{
					Stopwatch watch = new();
					watch.Start();

					for (int k = 0; k < ProjectileID.Count; k++)
					{
						try
						{
							Main.Assets.Request<Texture2D>(TextureAssets.Projectile[k].Name, AssetRequestMode.AsyncLoad).Wait();
						}
						catch
						{
							Logger.Warn($"Projectile asset {k} failed to load");
							continue;
						}
					}

					watch.Stop();
					Logger.Info($"Projectile assets finished loading in {watch.ElapsedMilliseconds} ms");
				})
				{
					IsBackground = true
				};
				projThread.Start();

				var npcThread = new Thread(() =>
				{
					Stopwatch watch = new();
					watch.Start();

					for (int k = 0; k < NPCID.Count; k++)
					{
						try
						{
							Main.Assets.Request<Texture2D>(TextureAssets.Npc[k].Name, AssetRequestMode.AsyncLoad).Wait();
						}
						catch
						{
							Logger.Warn($"NPC asset {k} failed to load");
							continue;
						}
					}

					watch.Stop();
					Logger.Info($"NPC assets finished loading in {watch.ElapsedMilliseconds} ms");
				})
				{
					IsBackground = true
				};
				npcThread.Start();

				var tileThread = new Thread(() =>
				{
					Stopwatch watch = new();
					watch.Start();

					for (int k = 0; k < TileID.Count; k++)
					{
						try
						{
							Main.Assets.Request<Texture2D>(TextureAssets.Tile[k].Name, AssetRequestMode.AsyncLoad).Wait();
						}
						catch
						{
							Logger.Warn($"Tile asset {k} failed to load");
							continue;
						}
					}

					watch.Stop();
					Logger.Info($"Tile assets finished loading in {watch.ElapsedMilliseconds} ms");
				})
				{
					IsBackground = true
				};
				tileThread.Start();
			}
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			string type = reader.ReadString();

			if (type == "ToolPacket")
				ToolHandler.HandlePacket(reader, whoAmI);

			if (type == "AdminUpdate")
				PermissionHandler.HandlePacket(reader);

			if (type == "ToolDataRequest")
				PermissionHandler.SendToolData(whoAmI);
		}
	}
}