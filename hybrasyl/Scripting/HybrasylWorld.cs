﻿/*
 * This file is part of Project Hybrasyl.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful, but
 * without ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE. See the Affero General Public License
 * for more details.
 *
 * You should have received a copy of the Affero General Public License along
 * with this program. If not, see <http://www.gnu.org/licenses/>.
 *
 * (C) 2013 Justin Baugh (baughj@hybrasyl.com)
 * (C) 2015-2016 Project Hybrasyl (info@hybrasyl.com)
 *
 * For contributors and individual authors please refer to CONTRIBUTORS.MD.
 * 
 */

using Hybrasyl.Dialogs;
using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;

namespace Hybrasyl.Scripting
{

    [MoonSharpUserData]
    public class HybrasylDialogOptions
    {
        public OrderedDictionary Options;

        public HybrasylDialogOptions()
        {
            Options = new OrderedDictionary();
        }

        public void AddOption(string option, string luaExpr=null)
        {
            Options.Add(option, luaExpr);
        }

        public void AddOption(string option, HybrasylDialog nextDialog)
        {
            if (nextDialog.DialogType == typeof(JumpDialog))
                Options.Add(option, nextDialog);
            else
                GameLog.Error($"Dialog option {option}: unsupported dialog type {nextDialog.DialogType.Name}");
        }
    }

    [MoonSharpUserData]
    public class HybrasylWorld
    {

        internal World World { get; set; }

        public HybrasylWorld(World world)
        {
            World = world;
        }

        public void WriteLog(string message)
        {
            GameLog.Info(message);
        }

        public int CurrentInGameYear => HybrasylTime.CurrentYear;
        public string CurrentInGameAge => HybrasylTime.CurrentAgeName;

        public HybrasylTime CurrentTime()
        {
            var ht = HybrasylTime.Now;
            return ht;
        }

        public HybrasylDialogOptions NewDialogOptions() => new HybrasylDialogOptions();

        public HybrasylDialogSequence NewDialogSequence(string sequenceName, params object[] list)
        {
            var dialogSequence = new HybrasylDialogSequence(sequenceName);
            foreach (var entry in list)
            {
                GameLog.InfoFormat("Type is {0}", entry.GetType().ToString());
                if (entry is HybrasylDialog)
                {
                    var newdialog = entry as HybrasylDialog;
                    dialogSequence.AddDialog(newdialog);
                    newdialog.Sequence = dialogSequence.Sequence;
                }
                else
                {
                    GameLog.Error($"Unknown parameter type {entry.GetType()} passed to NewDialogSequence, ignored");
                }
            }
            return dialogSequence;
        }

        public HybrasylDialog NewDialog(string displayText, string callback = null)
        {
            var dialog = new SimpleDialog(displayText);
            dialog.SetCallbackHandler(callback);
            return new HybrasylDialog(dialog);
        }

        public HybrasylDialog NewTextDialog(string displayText, string topCaption, string bottomCaption, int inputLength = 254, string callback="", string handler="")
        {
            var dialog = new TextDialog(displayText, topCaption, bottomCaption, inputLength);
            dialog.SetInputHandler(handler);
            dialog.SetCallbackHandler(callback);
            return new HybrasylDialog(dialog);
        }

        public HybrasylDialog NewOptionsDialog(string displayText, HybrasylDialogOptions dialogOptions, string callback="", string handler = "")
        {
            var dialog = new OptionsDialog(displayText);
            foreach (DictionaryEntry entry in dialogOptions.Options)
            {
                if (entry.Value is string)
                    // Callback
                    dialog.AddDialogOption(entry.Key as string, entry.Value as string);
                else if (entry.Value is HybrasylDialog)
                {
                    var hd = entry.Value as HybrasylDialog;
                    if (hd.DialogType == typeof(JumpDialog))
                        // Dialog jump
                        dialog.AddDialogOption(entry.Key as string, hd.Dialog as JumpDialog);
                    else
                        GameLog.Error("Unknown dialog type {0} in NewOptionsDialog - only JumpDialog is allowed currently");
                }
                else if (entry.Value is null)
                    // This is JUST an option, with no callback or jump dialog. The dialog handler will process the option itself.
                    dialog.AddDialogOption(entry.Key as string);
                else
                    GameLog.Error($"Unknown type {entry.Value.GetType().Name} passed as argument to NewOptionsDialog call");
            }
            if (dialog.OptionCount == 0)
                GameLog.Warning($"OptionsDialog with no options created. This dialog WILL NOT render. DisplayText follows: {displayText}");
            dialog.SetInputHandler(handler);
            dialog.SetCallbackHandler(callback);
            return new HybrasylDialog(dialog);
        }

        public HybrasylDialog NewFunctionDialog(string luaExpr)
        {
            return new HybrasylDialog(new FunctionDialog(luaExpr));
        }

        public HybrasylDialog NewJumpDialog(string targetSequence)
        {
            var dialog = new JumpDialog(targetSequence);
            return new HybrasylDialog(dialog);
        }

        public HybrasylSpawn NewSpawn(string creaturename, string spawnname)
        {
            var spawn = new HybrasylSpawn(creaturename, spawnname);
            return spawn;
        }

        public void RegisterGlobalSequence(HybrasylDialogSequence globalSequence)
        {
            Game.World.RegisterGlobalSequence(globalSequence.Sequence);
        }
    }
}
