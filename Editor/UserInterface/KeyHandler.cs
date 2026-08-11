/**************************************************************************************************************************
* Copyright 2024, Peter R. Nelson
*
* This file is part of the RealmStudio application. The RealmStudio application is intended
* for creating fantasy maps for gaming and world building.
*
* RealmStudio is free software: you can redistribute it and/or modify it under the terms
* of the GNU General Public License as published by the Free Software Foundation,
* either version 3 of the License, or (at your option) any later version.
*
* This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
* without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
* See the GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License along with this program.
* The text of the GNU General Public License (GPL) is found in the LICENSE.txt file.
* If the LICENSE.txt file is not present or the text of the GNU GPL is not present in the LICENSE.txt file,
* see https://www.gnu.org/licenses/.
*
* For questions about the RealmStudio application or about licensing, please email
* support@brookmonte.com
*
***************************************************************************************************************************/
using RealmStudioShapeRenderingLib;
using RealmStudioX.Core;
using System.Windows.Input;

namespace RealmStudioX.WPF.Editor.UserInterface
{
    internal sealed class KeyHandler
    {
        // TODO: refactor to pass in SelectionService
        internal static bool HandleKey(EditorController editor, Key keyCode, ModifierKeys KeyModifiers)
        {
            ArgumentNullException.ThrowIfNull(editor);

            bool handled = false;

            bool isArrowKey =
                keyCode == Key.Up ||
                keyCode == Key.Down ||
                keyCode == Key.Left ||
                keyCode == Key.Right;

            if (!isArrowKey)
            {
                editor.CommitSymbolNudge();
                editor.CommitLabelNudge();
            }

            switch (keyCode)
            {
                case Key.Escape:
                    {
                        editor.Reset();
                        handled = true;
                    }
                    break;
                case Key.Delete:
                    {
                        if (editor.SelectionService!.SelectionCount == 1)
                        {
                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is Landform lf)
                            {
                                MapLayer landformLayer = MapBuilder.GetMapLayerByIndex(editor.Scene!.Map, MapBuilder.LANDFORMLAYER);

                                Cmd_ModifyLandforms cmd = new(landformLayer);

                                cmd.RegisterRemovedLandform(lf);

                                editor.Commands.Execute(cmd);

                                editor.SelectionService.ClearSelection();

                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is WaterSystem ws)
                            {
                                Cmd_ModifyWaterBodies cmd = new(editor.Scene!);
                                cmd.RegisterRemovedWaterSystem(ws);

                                editor.Commands.Execute(cmd);

                                editor.SelectionService.ClearSelection();

                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is WaterBody wb)
                            {
                                Cmd_ModifyWaterBodies cmd = new(editor.Scene!);
                                cmd.RegisterRemovedWaterBody(wb);

                                editor.Commands.Execute(cmd);

                                editor.SelectionService.ClearSelection();

                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapSymbol symbol)
                            {
                                MapLayer symbolLayer = MapBuilder.GetMapLayerByIndex(editor.Scene!.Map, MapBuilder.SYMBOLLAYER);

                                // create the command and force execution
                                Cmd_ModifySymbols cmd = new(symbolLayer, true);
                                cmd.RegisterRemovedSymbol(symbol);

                                editor.Commands.Execute(cmd);

                                editor.SelectionService.ClearSelection();

                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapPath path)
                            {
                                MapLayer pathLowerLayer = MapBuilder.GetMapLayerByIndex(editor.Scene!.Map, MapBuilder.PATHLOWERLAYER);
                                MapLayer pathUpperLayer = MapBuilder.GetMapLayerByIndex(editor.Scene!.Map, MapBuilder.PATHUPPERLAYER);

                                MapLayer layer = pathLowerLayer;
                                if (path.DrawOverSymbols)
                                {
                                    layer = pathUpperLayer;
                                }

                                // create the command and force execution
                                Cmd_ModifyMapPaths cmd = new(editor.Scene.Map, layer);
                                cmd.RegisterRemovedMapPath(path);

                                editor.Commands.Execute(cmd);

                                editor.SelectionService.ClearSelection();

                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapLabel label)
                            {
                                MapLayer labelLayer = MapBuilder.GetMapLayerByIndex(editor.Scene!.Map, MapBuilder.LABELLAYER);

                                // create the command and force execution
                                Cmd_ModifyLabels cmd = new(labelLayer);
                                cmd.RegisterRemovedLabel(label);

                                editor.Commands.Execute(cmd);

                                editor.SelectionService.ClearSelection();

                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is PlacedMapBox box)
                            {
                                MapLayer boxLayer = MapBuilder.GetMapLayerByIndex(editor.Scene!.Map, MapBuilder.BOXLAYER);

                                // create the command and force execution
                                Cmd_ModifyBoxes cmd = new(boxLayer);
                                cmd.RegisterRemovedBox(box);

                                editor.Commands.Execute(cmd);

                                editor.SelectionService.ClearSelection();

                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is IDrawnMapComponent)
                            {
                                // a drawn map component can be on any layer, so use Cmd_RemoveMapShapes to remove it
                                // as it doesn't require a specific layer to be specified
                                Cmd_RemoveMapShapes cmd = new(editor.Scene!, editor.SelectionService.SelectedObjects);

                                editor.Commands.Execute(cmd);

                                editor.SelectionService.ClearSelection();

                                return true;
                            }
                        }
                        else if (editor.SelectionService!.SelectionCount > 1)
                        {
                            // multiple objects selected
                            Cmd_RemoveMapShapes cmd = new(editor.Scene!, editor.SelectionService.SelectedObjects);

                            editor.Commands.Execute(cmd);

                            editor.SelectionService.ClearSelection();

                            return true;
                        }

                    }
                    break;
                case Key.PageUp:
                    {
                        MapLayer symbolLayer = MapBuilder.GetMapLayerByIndex(editor.Scene!.Map, MapBuilder.SYMBOLLAYER);

                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        {
                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapSymbol symbol)
                            {
                                symbolLayer.MoveMapComponentZOrder(symbol, ZOrderMoveType.ForwardStep);
                                return true;
                            }
                        }
                        else
                        {
                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapSymbol symbol)
                            {
                                symbolLayer.MoveMapComponentZOrder(symbol, ZOrderMoveType.ForwardOne);
                                return true;
                            }
                        }
                    }
                    break;
                case Key.PageDown:
                    {
                        MapLayer symbolLayer = MapBuilder.GetMapLayerByIndex(editor.Scene!.Map, MapBuilder.SYMBOLLAYER);

                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        {
                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapSymbol symbol)
                            {
                                symbolLayer.MoveMapComponentZOrder(symbol, ZOrderMoveType.BackwardStep);
                                return true;
                            }
                        }
                        else
                        {
                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapSymbol symbol)
                            {
                                symbolLayer.MoveMapComponentZOrder(symbol, ZOrderMoveType.BackwardOne);
                                return true;
                            }
                        }
                    }
                    break;
                case Key.B:
                    {
                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) &&
                            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        {
                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapSymbol symbol)
                            {
                                MapLayer symbolLayer = MapBuilder.GetMapLayerByIndex(editor.Scene!.Map, MapBuilder.SYMBOLLAYER);

                                symbolLayer.MoveMapComponentZOrder(symbol, ZOrderMoveType.ToBottom);
                                return true;
                            }
                        }
                    }
                    break;
                case Key.F:
                    {
                        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) &&
                            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        {
                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapSymbol symbol)
                            {
                                MapLayer symbolLayer = MapBuilder.GetMapLayerByIndex(editor.Scene!.Map, MapBuilder.SYMBOLLAYER);

                                symbolLayer.MoveMapComponentZOrder(symbol, ZOrderMoveType.ToTop);
                                return true;
                            }
                        }
                    }
                    break;
                case Key.Home:
                    {
                        if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapSymbol symbol)
                        {
                            MapLayer symbolLayer = MapBuilder.GetMapLayerByIndex(editor.Scene!.Map, MapBuilder.SYMBOLLAYER);

                            symbolLayer.MoveMapComponentZOrder(symbol, ZOrderMoveType.AboveAllOverlaps);
                            return true;
                        }
                    }
                    break;
                case Key.End:
                    {
                        if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapSymbol symbol)
                        {
                            MapLayer symbolLayer = MapBuilder.GetMapLayerByIndex(editor.Scene!.Map, MapBuilder.SYMBOLLAYER);

                            symbolLayer.MoveMapComponentZOrder(symbol, ZOrderMoveType.BelowAllOverlaps);
                            return true;
                        }
                    }
                    break;
                case Key.Up:
                    {
                        if (editor.SelectionService!.PrimarySelection != null)
                        {
                            // TODO: nudging of landforms; any other object types?
                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapSymbol symbol)
                            {
                                editor.NudgeSymbol(symbol, Keys.Up, 0, -1);
                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapLabel label)
                            {
                                editor.NudgeLabel(label, Keys.Up, 0, -1);
                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is PlacedMapBox box)
                            {
                                editor.NudgeBox(box, Keys.Up, 0, -1);
                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is IDrawnMapComponent dmc)
                            {
                                editor.NudgeDrawnMapComponent(dmc, Keys.Up, 0, -1);
                                return true;
                            }
                        }
                    }
                    break;
                case Key.Down:
                    {
                        if (editor.SelectionService!.PrimarySelection != null)
                        {
                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapSymbol symbol)
                            {
                                editor.NudgeSymbol(symbol, Keys.Down, 0, +1);
                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapLabel label)
                            {
                                editor.NudgeLabel(label, Keys.Down, 0, +1);
                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is PlacedMapBox box)
                            {
                                editor.NudgeBox(box, Keys.Down, 0, +1);
                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is IDrawnMapComponent dmc)
                            {
                                editor.NudgeDrawnMapComponent(dmc, Keys.Up, 0, +1);
                                return true;
                            }
                        }    
                    }
                    break;
                case Key.Left:
                    {
                        if (editor.SelectionService!.PrimarySelection != null)
                        {
                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapSymbol symbol)
                            {
                                editor.NudgeSymbol(symbol, Keys.Left, -1, 0);
                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapLabel label)
                            {
                                editor.NudgeLabel(label, Keys.Left, -1, 0);
                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is PlacedMapBox box)
                            {
                                editor.NudgeBox(box, Keys.Left, -1, 0);
                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is IDrawnMapComponent dmc)
                            {
                                editor.NudgeDrawnMapComponent(dmc, Keys.Up, -1, 0);
                                return true;
                            }
                        }
                    }
                    break;
                case Key.Right:
                    {
                        if (editor.SelectionService!.PrimarySelection != null)
                        {
                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapSymbol symbol)
                            {
                                editor.NudgeSymbol(symbol, Keys.Right, +1, 0);
                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is MapLabel label)
                            {
                                editor.NudgeLabel(label, Keys.Right, +1, 0);
                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is PlacedMapBox box)
                            {
                                editor.NudgeBox(box, Keys.Right, +1, 0);
                                return true;
                            }

                            if (editor.SelectionService!.PrimarySelection != null && editor.SelectionService!.PrimarySelection.ReferencedShape is IDrawnMapComponent dmc)
                            {
                                editor.NudgeDrawnMapComponent(dmc, Keys.Up, +1, 0);
                                return true;
                            }
                        }
                    }
                    break;
            }

            return handled;
        }

        public static ModifierKeys GetModifiers()
        {
            ModifierKeys result = ModifierKeys.None;

            var wfMods = System.Windows.Forms.Control.ModifierKeys;

            if ((wfMods & System.Windows.Forms.Keys.Control) != 0)
                result |= ModifierKeys.Control;

            if ((wfMods & System.Windows.Forms.Keys.Shift) != 0)
                result |= ModifierKeys.Shift;

            if ((wfMods & System.Windows.Forms.Keys.Alt) != 0)
                result |= ModifierKeys.Alt;

            return result;
        }
    }
}
