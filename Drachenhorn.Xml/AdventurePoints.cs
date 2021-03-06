﻿using System;
using System.Xml.Serialization;

namespace Drachenhorn.Xml
{
    /// <summary>
    ///     Character Adventure Points
    /// </summary>
    /// <seealso cref="Drachenhorn.Xml.BindableBase" />
    [Serializable]
    public class AdventurePoints : BindableBase
    {
        #region Properties

        [XmlIgnore] private int _total;

        [XmlIgnore] private int _used;

        /// <summary>
        ///     Gets or sets the total points.
        /// </summary>
        /// <value>
        ///     The total points.
        /// </value>
        [XmlAttribute("Total")]
        public int Total
        {
            get => _total;
            set
            {
                if (_total == value)
                    return;
                _total = value;
                OnPropertyChanged(null);
            }
        }

        /// <summary>
        ///     Gets or sets the used points.
        /// </summary>
        /// <value>
        ///     The used points.
        /// </value>
        [XmlAttribute("Used")]
        public int Used
        {
            get => _used;
            set
            {
                if (_used == value)
                    return;
                _used = value;
                OnPropertyChanged(null);
            }
        }

        /// <summary>
        ///     Gets the current left points.
        /// </summary>
        /// <value>
        ///     The current left points.
        /// </value>
        [XmlIgnore]
        public int CurrentLeft => Total - Used;

        #endregion
    }
}