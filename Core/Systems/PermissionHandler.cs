﻿using DragonLens.Core.Systems.ToolSystem;
using DragonLens.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria.ID;

namespace DragonLens.Core.Systems
{
	internal class PermissionHandler : ModSystem
	{
		public static readonly List<string> admins = new();

		/// <summary>
		/// Determines if a player can use tools or not, based on their netmode and admin status.
		/// </summary>
		/// <param name="player">The player to query for usability.</param>
		/// <returns>If that player should be able to use tools or not.</returns>
		public static bool CanUseTools(Player player)
		{
			if (Main.netMode == NetmodeID.SinglePlayer)
				return true;
			else
				return admins.Contains(player.name);
		}

		/// <summary>
		/// This method adds a player to the list of admins, which can use tools.
		/// </summary>
		/// <param name="player">The player to add. They must not already be an admin.</param>
		public static void AddAdmin(Player player)
		{
			admins.Add(player.name);

			ModPacket packet = ModLoader.GetMod("DragonLens").GetPacket();
			packet.Write("AdminUpdate");
			packet.Write(0);
			packet.Write(player.name);
			packet.Send();
		}

		/// <summary>
		/// This method removes a player from the list of admins, preventing them from using tools.
		/// </summary>
		/// <param name="player">The player to remove. They must be an admin.</param>
		public static void RemoveAdmin(Player player)
		{
			if (!admins.Contains(player.name))
				return;

			admins.Remove(player.name);

			ModPacket packet = ModLoader.GetMod("DragonLens").GetPacket();
			packet.Write("AdminUpdate");
			packet.Write(1);
			packet.Write(player.name);
			packet.Send();
		}

		/// <summary>
		/// Called by the server to send the servers current tool data to new clients
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="sender"></param>
		public static void SendToolData(int sender)
		{
			if (Main.netMode != NetmodeID.Server)
			{
				ModLoader.GetMod("DragonLens").Logger.Error("Client recieved a request intended for the server!");
				return;
			}

			foreach (Tool tool in ToolHandler.Tools)
			{
				tool.NetSend(sender, -1);
			}
		}

		/// <summary>
		/// Handles an admin status update packet from the net
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="sender"></param>
		public static void HandlePacket(BinaryReader reader)
		{
			int operation = reader.ReadInt32(); //First read the operation type

			if (operation == 0) //Set admin
			{
				string name = reader.ReadString();
				Player player = Main.player.FirstOrDefault(n => n.name == name);

				if (player is null)
					Main.NewText(LocalizationHelper.GetText("Permission.PlayerNotFound", name));

				if (player == Main.LocalPlayer)
					Main.NewText(LocalizationHelper.GetText("Permission.AdminGiven"), Color.LimeGreen);

				admins.Add(player.name);

				if (Main.netMode == NetmodeID.Server)
				{
					ModPacket packet = ModLoader.GetMod("DragonLens").GetPacket();
					packet.Write("AdminUpdate");
					packet.Write(operation);
					packet.Write(player.name);
					packet.Send();
				}
			}
			else if (operation == 1) //Remove admin
			{
				string name = reader.ReadString();
				Player player = Main.player.FirstOrDefault(n => n.name == name);

				if (player is null)
					Main.NewText(LocalizationHelper.GetText("Permission.PlayerNotFound", name));

				if (player == Main.LocalPlayer)
					Main.NewText(LocalizationHelper.GetText("Permission.AdminTaken"), Color.Red);

				admins.Remove(player.name);

				if (Main.netMode == NetmodeID.Server)
				{
					ModPacket packet = ModLoader.GetMod("DragonLens").GetPacket();
					packet.Write("AdminUpdate");
					packet.Write(operation);
					packet.Write(player.name);
					packet.Send();
				}
			}
			else if (operation == 2) //Sync only
			{
				if (Main.netMode == NetmodeID.Server)
				{
					ModPacket packet = ModLoader.GetMod("DragonLens").GetPacket();
					packet.Write("AdminUpdate");
					packet.Write(operation);
					packet.Send();
				}
			}
		}
	}

	/// <summary>
	/// Handles clients getting the correct info on connection
	/// </summary>
	public class PermissionPlayer : ModPlayer
	{
		public override void OnEnterWorld() // Send an admin list sync request on entering the server
		{
			if (Main.netMode == NetmodeID.SinglePlayer) //single player dosent care about admins
				return;

			if (Main.netMode == NetmodeID.MultiplayerClient) // Sync the admin info!
			{

				ModPacket packet = ModLoader.GetMod("DragonLens").GetPacket();
				packet.Write("AdminUpdate");
				packet.Write(2);
				packet.Send();
			}

			if (Netplay.Connection.Socket.GetRemoteAddress().IsLocalHost()) // The host is automatically an admin!
			{
				PermissionHandler.AddAdmin(Player);

				foreach (Tool tool in ToolHandler.Tools) // The hosts settings get applied
				{
					tool.NetSend();
				}
			}
			else // Otherwise ask for the servers tool data
			{
				ModPacket packet = ModLoader.GetMod("DragonLens").GetPacket();
				packet.Write("ToolDataRequest");
				packet.Send();
			}
		}
	}

	/// <summary>
	/// A base class to make localization of admin power commands more convenient
	/// </summary>
	internal abstract class PermissionCommand : ModCommand
	{
		public override string Usage => LocalizationHelper.GetText($"Permission.{GetType().Name}.Usage");

		public override string Description => LocalizationHelper.GetText($"Permission.{GetType().Name}.Description");

		protected string GetText(string key, params object[] args)
		{
			return LocalizationHelper.GetText($"Permission.{GetType().Name}.{key}", args);
		}
	}

	/// <summary>
	/// A command to grant admin status to a player
	/// </summary>
	internal class AdminCommand : PermissionCommand
	{
		public override string Command => "DLAdmin";

		public override CommandType Type => CommandType.Server | CommandType.Console;

		public override void Action(CommandCaller caller, string input, string[] args)
		{
			if (caller.CommandType != CommandType.Console && !PermissionHandler.CanUseTools(caller.Player)) //Only admins or console can make more admins
				return;

			if (args.Length < 1)
			{
				Console.WriteLine(GetText("NameNotEntered"));
				return;
			}

			string name = args[0];

			if (PermissionHandler.admins.Contains(name))
			{
				Console.WriteLine(GetText("AlreadyAdmin"));
				return;
			}

			Player player = Main.player.FirstOrDefault(n => n.name == name);

			if (player is null)
			{
				Console.WriteLine(LocalizationHelper.GetText("Permission.PlayerNotFound", name));
			}
			else
			{
				Console.WriteLine(GetText("AdminGiven", name));
				PermissionHandler.AddAdmin(player);
			}
		}
	}

	/// <summary>
	/// A command to revoke admin status from a player
	/// </summary>
	internal class DeAdminCommand : PermissionCommand
	{
		public override string Command => "DLRemoveAdmin";

		public override CommandType Type => CommandType.Server | CommandType.Console;

		public override string Usage => "/DLRemoveAdmin [player name]";

		public override string Description => "Removes a user from the admin list for DragonLens, disallowing them from using the mods cheat tools.";

		public override void Action(CommandCaller caller, string input, string[] args)
		{
			if (caller.CommandType != CommandType.Console && !PermissionHandler.CanUseTools(caller.Player)) //Only admins or console can remove admins
				return;

			if (args.Length < 1)
			{
				Console.WriteLine(GetText("NameNotEntered"));
				return;
			}

			string name = args[0];

			if (!PermissionHandler.admins.Contains(name))
			{
				Console.WriteLine(GetText("NotAdmin"));
				return;
			}

			Player player = Main.player.FirstOrDefault(n => n.name == name);

			if (player is null)
			{
				Console.WriteLine(LocalizationHelper.GetText("Permission.PlayerNotFound", name));
			}
			else
			{
				Console.WriteLine(GetText("AdminTaken", name));
				PermissionHandler.RemoveAdmin(player);
			}
		}
	}

	/// <summary>
	/// A command to list all current admins on the server
	/// </summary>
	internal class AdminListCommand : ModCommand
	{
		public override string Command => "DLAdminList";

		public override CommandType Type => CommandType.Chat | CommandType.Console;

		public override string Description => LocalizationHelper.GetText("Permission.AdminListCommand.Description");

		public override void Action(CommandCaller caller, string input, string[] args)
		{
			foreach (string name in PermissionHandler.admins)
			{
				if (caller.CommandType == CommandType.Console)
					Console.WriteLine($"{name}");
				else
					Main.NewText($"{name}");
			}
		}
	}
}