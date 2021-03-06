﻿using System;
using System.Collections.ObjectModel;
using Drachenhorn.Core.IO;
using Drachenhorn.Core.Printing;
using Drachenhorn.Core.UI;
using Drachenhorn.Core.ViewModels.Sheet;
using Drachenhorn.Xml.Exceptions;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;

namespace Drachenhorn.Core.ViewModels.Common
{
    public class MainViewModel : ViewModelBase
    {
        #region Properties

        private ObservableCollection<CharacterSheetViewModel> _characterSheetViewModels =
            new ObservableCollection<CharacterSheetViewModel>();

        private CharacterSheetViewModel _currentSheetViewModel;

        public ObservableCollection<CharacterSheetViewModel> CharacterSheetViewModels
        {
            get => _characterSheetViewModels;
            set
            {
                if (_characterSheetViewModels == value)
                    return;
                _characterSheetViewModels = value;
                RaisePropertyChanged();
            }
        }

        public CharacterSheetViewModel CurrentSheetViewModel
        {
            get => _currentSheetViewModel;
            set
            {
                if (_currentSheetViewModel == value)
                    return;
                _currentSheetViewModel = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Commands

        public RelayCommand Save => new RelayCommand(ExecuteSave);

        private void ExecuteSave()
        {
            if (!string.IsNullOrEmpty(CurrentSheetViewModel?.CurrentSheet?.FilePath))
                try
                {
                    CurrentSheetViewModel.Save();
                }
                catch (SheetSavingException ex)
                {
                    Messenger.Default.Send<Exception>(ex);
                }
            else
                ExecuteSaveAs();
        }

        public RelayCommand SaveAs => new RelayCommand(ExecuteSaveAs);

        private void ExecuteSaveAs()
        {
            CurrentSheetViewModel?.SaveAs();
        }

        public RelayCommand SaveAll => new RelayCommand(ExecuteSaveAll);

        private void ExecuteSaveAll()
        {
            foreach (var model in CharacterSheetViewModels) model.Save();
        }

        public RelayCommand Open => new RelayCommand(ExecuteOpen);

        private void ExecuteOpen()
        {
            var ioService = SimpleIoc.Default.GetInstance<IIoService>();

            try
            {
                var temp = ioService.OpenCharacterSheet();

                if (temp == null)
                    return;

                var model = new CharacterSheetViewModel(temp);
                CharacterSheetViewModels.Add(model);
                CurrentSheetViewModel = model;
            }
            catch (SheetLoadingException ex)
            {
                Messenger.Default.Send<Exception>(ex);
            }
        }

        public RelayCommand New => new RelayCommand(ExecuteNew);

        private void ExecuteNew()
        {
            var model = new CharacterSheetViewModel();
            CharacterSheetViewModels.Add(model);
            CurrentSheetViewModel = model;
        }

        public RelayCommand Print => new RelayCommand(ExecutePrint);

        private void ExecutePrint()
        {
            Messenger.Default.Send(new NotificationMessage(this, "ShowPrintView"));
        }

        public RelayCommand GeneratePDF => new RelayCommand(ExecuteGeneratePDF);

        private async void ExecuteGeneratePDF()
        {
            if (CurrentSheetViewModel?.CurrentSheet == null)
                await MessageFactory.NewMessage()
                    .MessageTranslated("Dialog.NothingSelected")
                    .Title("Dialog.NothingSelected_Caption")
                    .ShowMessage();

            SimpleIoc.Default.GetInstance<IUIService>().SetBusyState();

            await PrintingManager.GeneratePDFAsync(CurrentSheetViewModel?.CurrentSheet);
        }

        public RelayCommand<CharacterSheetViewModel> Close => new RelayCommand<CharacterSheetViewModel>(ExecuteClose);

        private async void ExecuteClose(CharacterSheetViewModel model)
        {
            if (model == null)
                model = CurrentSheetViewModel;

            if (!model.CurrentSheet.HasChanged)
            {
                CharacterSheetViewModels.Remove(model);
                return;
            }

            var result = await MessageFactory.NewMessage()
                .MessageTranslated("Dialog.ShouldClose")
                .TitleTranslated("Dialog.ShouldClose_Caption")
                .ButtonTranslated("Dialog.Yes", 0)
                .ButtonTranslated("Dialog.No")
                .ShowMessage();

            if (result == 0)
                CharacterSheetViewModels.Remove(model);
        }

        #endregion Commands
    }
}