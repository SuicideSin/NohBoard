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

namespace ThoNohT.NohBoard.Keyboard
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using ElementDefinitions;
    using ThoNohT.NohBoard.Extra;

    /// <summary>
    /// Represents a keyboard, can be serialized to a keyboard file.
    /// </summary>
    [DataContract(Name = "Keyboard", Namespace = "")]
    public class KeyboardDefinition
    {
        #region Properties

        /// <summary>
        /// A friendly name of the keyboard.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The category of the keyboard.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// The version of the keyboard.
        /// </summary>
        [DataMember]
        public int Version { get; set; }

        /// <summary>
        /// The width of the keyboard, in pixels.
        /// </summary>
        [DataMember]
        public int Width { get; set; }

        /// <summary>
        /// The height of the keyboard, in pixels.
        /// </summary>
        [DataMember]
        public int Height { get; set; }

        /// <summary>
        /// The list of elements defined in this keyboard.
        /// </summary>
        [DataMember]
        public List<ElementDefinition> Elements { get; set; }

        #endregion Properties

        /// <summary>
        /// Calculates the bounding box of all elements in the keyboard definition.
        /// </summary>
        /// <returns>The calculated bounding box.</returns>
        public Rectangle GetBoundingBox()
        {
            var minX = this.Elements.Select(x => x.GetBoundingBox().X).Min();
            var minY = this.Elements.Select(x => x.GetBoundingBox().Y).Min();

            return new Rectangle(
                new Point(minX, minY),
                new Size(
                    this.Elements.Select(x => x.GetBoundingBox().Right).Max() - minX,
                    this.Elements.Select(x => x.GetBoundingBox().Bottom).Max() - minY));
        }

        public void Save()
        {
            var filename = Path.Combine(
                Constants.ExePath,
                Constants.KeyboardsFolder,
                this.Category,
                this.Name,
                Constants.DefinitionFilename);

            FileHelper.EnsurePathExists(filename);
            FileHelper.Serialize(filename, this);
        }

        public static KeyboardDefinition Load(string category, string name)
        {
            var categoryPath = Path.Combine(Constants.ExePath, Constants.KeyboardsFolder, category);
            if (!Directory.Exists(categoryPath))
                throw new ArgumentException($"Category {category} does not exist.");

            var keyboardPath = Path.Combine(categoryPath, name);
            if (!Directory.Exists(keyboardPath))
                throw new ArgumentException($"Keyboard {name} does not exist.");

            var filePath = Path.Combine(keyboardPath, Constants.DefinitionFilename);
            if (!File.Exists(filePath))
                throw new Exception($"Keyboard definition file not found for {category}/{name}.");

            var kbDef = FileHelper.Deserialize<KeyboardDefinition>(filePath);
            kbDef.Category = category;
            kbDef.Name = name;
            return kbDef;
        }
    }
}