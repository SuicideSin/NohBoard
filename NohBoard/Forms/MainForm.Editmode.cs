﻿/*
Copyright (C) 2016 by Eric Bataille <e.c.p.bataille@gmail.com>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/


namespace ThoNohT.NohBoard.Forms
{
    using Extra;
    using Hooking;
    using Keyboard;
    using Keyboard.ElementDefinitions;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    /// <summary>
    /// Edit mode part of the main form.
    /// </summary>
    public partial class MainForm
    {
        #region Manipulations

        /// <summary>
        /// The keyboard element that is currently being manipulated.
        /// </summary>
        private Tuple<int, ElementDefinition> currentlyManipulating = null;

        /// <summary>
        /// The currently manipulated element, at the point where the manipulation started.
        /// </summary>
        private ElementDefinition manipulationStart = null;

        /// <summary>
        /// The cumulative distance the current element has been manipulated.
        /// </summary>
        private Size cumulManipulation;

        /// <summary>
        /// The point inside <see cref="currentlyManipulating"/> that is being manipulated. This determines the type of
        /// manipulation that will be performed on the currently manipulating element.
        /// </summary>
        private Point currentManipulationPoint; 

        #endregion Manipulations

        /// <summary>
        /// The element that is currently highlighted by the mouse, but not being manipulated yet.
        /// </summary>
        private ElementDefinition HighlightedDefinition = null;

        /// <summary>
        /// A stack containing the previous edits made by the user.
        /// </summary>
        private readonly Stack<KeyboardDefinition> UndoHistory = new Stack<KeyboardDefinition>();

        /// <summary>
        /// A stack containing the recently undone edits.
        /// </summary>
        private readonly Stack<KeyboardDefinition> RedoHistory = new Stack<KeyboardDefinition>();

        /// <summary>
        /// Turns edit-mode on or off.
        /// </summary>
        private void mnuToggleEditMode_Click(object sender, EventArgs e)
        {
            this.mnuToggleEditMode.Text = this.mnuToggleEditMode.Checked ? "Stop Editing" : "Start Editing";

            if (!this.mnuToggleEditMode.Checked)
            {
                this.currentlyManipulating = null;
                this.HighlightedDefinition = null;
            }
        }

        /// <summary>
        /// Handles the MouseDown event for the main form, which can start editing an element, the mouse is pointing
        /// at one.
        /// </summary>
        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (!this.mnuToggleEditMode.Checked) return;

            var toManipulate = GlobalSettings.CurrentDefinition.Elements
                .LastOrDefault(x => x.StartManipulating(e.Location, KeyboardState.AltDown));

            if (toManipulate == null)
            {
                this.currentlyManipulating = null;
                return;
            }

            var indexToManipulate = GlobalSettings.CurrentDefinition.Elements.IndexOf(toManipulate);
            this.currentlyManipulating = Tuple.Create(indexToManipulate, toManipulate);
            this.HighlightedDefinition = null;

            // For edge movement.
            this.manipulationStart = toManipulate;
            this.cumulManipulation = new Size();

            this.UndoHistory.Push(GlobalSettings.CurrentDefinition);
            this.RedoHistory.Clear();
            GlobalSettings.CurrentDefinition = GlobalSettings.CurrentDefinition
                .RemoveElement(toManipulate);

            this.ResetBackBrushes();
        }

        /// <summary>
        /// Handles the MouseMove event for the main form, which performs all transformations that need to be done
        /// when editing an element.
        /// </summary>
        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (!this.mnuToggleEditMode.Checked) return;

            if (this.currentlyManipulating != null)
            {
                var diff = (TPoint)e.Location - this.currentManipulationPoint;
                this.cumulManipulation += diff;

                this.currentlyManipulating = Tuple.Create(
                    this.currentlyManipulating.Item1,
                    this.manipulationStart.Manipulate(this.cumulManipulation));
                this.currentManipulationPoint = e.Location;
            }
            else
            {
                this.currentManipulationPoint = e.Location;
                this.HighlightedDefinition = GlobalSettings.CurrentDefinition.Elements
                    .LastOrDefault(x => x.StartManipulating(e.Location, KeyboardState.AltDown));
            }
        }

        /// <summary>
        /// Handles the MouseUp event for the main form, which will stop editing an element.
        /// </summary>
        private void MainForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (!this.mnuToggleEditMode.Checked || this.currentlyManipulating == null) return;

            GlobalSettings.CurrentDefinition = GlobalSettings.CurrentDefinition.AddElement(
                this.currentlyManipulating.Item2,
                this.currentlyManipulating.Item1);

            this.currentlyManipulating = null;
            this.manipulationStart = null;
            this.currentManipulationPoint = new Point();
            this.ResetBackBrushes();
        }

        #region Element z-order moving

        /// <summary>
        /// Moves the currently highlighted element to the top of the z-order. Placing it above every other element.
        /// </summary>
        private void mnuMoveToTop_Click(object sender, EventArgs e)
        {
            GlobalSettings.CurrentDefinition = GlobalSettings.CurrentDefinition.MoveElementDown(
                this.HighlightedDefinition,
                int.MaxValue);
            this.ResetBackBrushes();
        }

        /// <summary>
        /// Moves the currently highlighted element up in the z-order.
        /// </summary>
        private void mnuMoveUp_Click(object sender, EventArgs e)
        {
            GlobalSettings.CurrentDefinition = GlobalSettings.CurrentDefinition.MoveElementDown(
                this.HighlightedDefinition,
                1);
            this.ResetBackBrushes();
        }

        /// <summary>
        /// Moves the currently highlighted element down in the z-order.
        /// </summary>
        private void mnuMoveDown_Click(object sender, EventArgs e)
        {
            GlobalSettings.CurrentDefinition = GlobalSettings.CurrentDefinition.MoveElementDown(
                this.HighlightedDefinition,
                -1);
            this.ResetBackBrushes();
        }

        /// <summary>
        /// Moves the currently highlighted element to the bottom of the z-order. Placing it below every other element.
        /// </summary>
        private void mnuMoveToBottom_Click(object sender, EventArgs e)
        {
            GlobalSettings.CurrentDefinition = GlobalSettings.CurrentDefinition.MoveElementDown(
                this.HighlightedDefinition,
                -int.MaxValue);
            this.ResetBackBrushes();
        }

        #endregion Element z-order moving

        #region Undo

        /// <summary>
        /// Handles the keyup event for the main form.
        /// </summary>
        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (!this.mnuToggleEditMode.Checked) return;
            if (!e.Control || e.KeyCode != Keys.Z) return;

            if (!e.Shift)
            {
                if (!this.UndoHistory.Any()) return;

                this.RedoHistory.Push(GlobalSettings.CurrentDefinition);
                GlobalSettings.CurrentDefinition = this.UndoHistory.Pop();
            }
            else
            {
                if (!this.RedoHistory.Any()) return;

                this.UndoHistory.Push(GlobalSettings.CurrentDefinition);
                GlobalSettings.CurrentDefinition = this.RedoHistory.Pop();
            }

            this.HighlightedDefinition = null;
            this.ResetBackBrushes();
        }

        #endregion Undo

        #region Element management

        /// <summary>
        /// Adds a new element definition to the current keyboard.
        /// </summary>
        /// <param name="definition">The definition to add.</param>
        private void AddElement(ElementDefinition definition)
        {
            if (!this.mnuToggleEditMode.Checked) return;
            if (this.HighlightedDefinition != null) return;

            this.UndoHistory.Push(GlobalSettings.CurrentDefinition);
            this.RedoHistory.Clear();

            GlobalSettings.CurrentDefinition = GlobalSettings.CurrentDefinition
                .AddElement(definition);

            this.ResetBackBrushes();
        }

        /// <summary>
        /// Creates a new keyboard key definition and adds it to the keyboard definition.
        /// </summary>
        private void mnuAddKeyboardKeyDefinition_Click(object sender, EventArgs e)
        {
            var c = this.currentManipulationPoint;
            var w = Constants.DefaultElementSize / 2;
            var boundaries = new List<TPoint>
            {
                new TPoint(c.X - w, c.Y - w), // Top left
                new TPoint(c.X + w, c.Y - w), // Top right
                new TPoint(c.X + w, c.Y + w), // Bottom right
                new TPoint(c.X - w, c.Y + w), // Bottom left
            };

            this.AddElement(
                new KeyboardKeyDefinition(
                    GlobalSettings.CurrentDefinition.GetNextId(),
                    boundaries,
                    new List<int>(),
                    "",
                    "",
                    true));
        }

        /// <summary>
        /// Creates a new mouse key definition and adds it to the keyboard definition.
        /// </summary>
        private void mnuAddMouseKeyDefinition_Click(object sender, EventArgs e)
        {
            var c = this.currentManipulationPoint;
            var w = Constants.DefaultElementSize / 2;
            var boundaries = new List<TPoint>
            {
                new TPoint(c.X - w, c.Y - w), // Top left
                new TPoint(c.X + w, c.Y - w), // Top right
                new TPoint(c.X + w, c.Y + w), // Bottom right
                new TPoint(c.X - w, c.Y + w), // Bottom left
            };

            this.AddElement(
                new MouseKeyDefinition(
                    GlobalSettings.CurrentDefinition.GetNextId(),
                    boundaries,
                    (int) MouseKeyCode.LeftButton,
                    ""));
        }

        /// <summary>
        /// Creates a new mouse scroll definition and adds it to the keyboard definition.
        /// </summary>
        private void mnuAddMouseScrollDefinition_Click(object sender, EventArgs e)
        {
            var c = this.currentManipulationPoint;
            var w = Constants.DefaultElementSize / 2;
            var boundaries = new List<TPoint>
            {
                new TPoint(c.X - w, c.Y - w), // Top left
                new TPoint(c.X + w, c.Y - w), // Top right
                new TPoint(c.X + w, c.Y + w), // Bottom right
                new TPoint(c.X - w, c.Y + w), // Bottom left
            };

            this.AddElement(
                new MouseScrollDefinition(
                    GlobalSettings.CurrentDefinition.GetNextId(),
                    boundaries,
                    (int) MouseScrollKeyCode.ScrollDown,
                    ""));
        }

        /// <summary>
        /// Creates a new mouse speed indicator definition and adds it to the keyboard definition.
        /// </summary>
        private void mnuAddMouseSpeedIndicatorDefinition_Click(object sender, EventArgs e)
        {
            this.AddElement(
                new MouseSpeedIndicatorDefinition(
                    GlobalSettings.CurrentDefinition.GetNextId(),
                    this.currentManipulationPoint,
                    Constants.DefaultElementSize / 2));
        }

        /// <summary>
        /// Removes an element from the current keyboard.
        /// </summary>
        private void mnuRemoveElement_Click(object sender, EventArgs e)
        {
            if (!this.mnuToggleEditMode.Checked) return;
            if (this.HighlightedDefinition == null) return;

            this.UndoHistory.Push(GlobalSettings.CurrentDefinition);
            this.RedoHistory.Clear();

            GlobalSettings.CurrentDefinition = GlobalSettings.CurrentDefinition
                .RemoveElement(this.HighlightedDefinition);

            // Unset the definition everywhere because it no longer exists.
            this.HighlightedDefinition = null;
            this.currentlyManipulating = null;
            this.manipulationStart = null;

            this.ResetBackBrushes();
        }

        #endregion Element management

        #region Boundary management

        /// <summary>
        /// Adds a boundary point to the highlighted element.
        /// </summary>
        private void mnuAddBoundaryPoint_Click(object sender, EventArgs e)
        {
            if (!this.mnuToggleEditMode.Checked) return;
            if (!(this.HighlightedDefinition is KeyDefinition)) return;

            this.UndoHistory.Push(GlobalSettings.CurrentDefinition);
            this.RedoHistory.Clear();

            var def = (KeyDefinition)this.HighlightedDefinition;
            var index = GlobalSettings.CurrentDefinition.Elements.IndexOf(def);

            GlobalSettings.CurrentDefinition = GlobalSettings.CurrentDefinition
                .RemoveElement(def)
                .AddElement(def.AddBoundary(this.currentManipulationPoint), index);

            this.ResetBackBrushes();
        }

        /// <summary>
        /// Removes a boundary point from the highighted element.
        /// </summary>
        private void mnuRemoveBoundaryPoint_Click(object sender, EventArgs e)
        {
            if (!this.mnuToggleEditMode.Checked) return;
            if (!(this.HighlightedDefinition is KeyDefinition)) return;

            var def = (KeyDefinition)this.HighlightedDefinition;
            if (def.Boundaries.Count < 4)
            {
                MessageBox.Show(
                    "You cannot remove another boundary, there must be at least 3.",
                    "Error removing boundary",
                    MessageBoxButtons.OK);
                return;
            }

            this.UndoHistory.Push(GlobalSettings.CurrentDefinition);
            this.RedoHistory.Clear();

            var index = GlobalSettings.CurrentDefinition.Elements.IndexOf(def);

            GlobalSettings.CurrentDefinition = GlobalSettings.CurrentDefinition
                .RemoveElement(def)
                .AddElement(def.RemoveBoundary(), index);

            this.ResetBackBrushes();
        }

        #endregion Boundary management
    }
}