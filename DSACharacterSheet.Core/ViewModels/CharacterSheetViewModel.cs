﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonServiceLocator;
using DSACharacterSheet.Core.IO;
using DSACharacterSheet.FileReader;
using DSACharacterSheet.FileReader.Exceptions;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;

namespace DSACharacterSheet.Core.ViewModels
{
    public class CharacterSheetViewModel : ViewModelBase
    {
        #region Properties

        private CharacterSheet _currentSheet = new CharacterSheet();

        public CharacterSheet CurrentSheet
        {
            get { return _currentSheet; }
            set
            {
                if (_currentSheet == value)
                    return;
                _currentSheet = value;
                RaisePropertyChanged();
            }
        }

        #endregion Properties


        #region c'tor

        public CharacterSheetViewModel()
        {
        }

        public CharacterSheetViewModel(CharacterSheet sheet)
        {
            CurrentSheet = sheet;
        }

        #endregion c'tor


        #region Actions

        public void Save(string path = null)
        {
            if (path == null)
                CurrentSheet.Save();
            else
                CurrentSheet.Save(path);
        }

        public void SaveAs()
        {
            var ioService = ServiceLocator.Current.GetInstance<IIOService>();

            try
            {
                ioService.SaveAsCharacterSheet(CurrentSheet);
            }
            catch (SheetSavingException ex)
            {
                Messenger.Default.Send<Exception>(ex);
            }
        }

        public void Open()
        {
            var ioService = ServiceLocator.Current.GetInstance<IIOService>();

            try
            {
                ioService.OpenCharacterSheet();
            }
            catch (SheetLoadingException ex)
            {
                Messenger.Default.Send<Exception>(ex);
            }
        }

        #endregion Actions
    }
}