using RealmStudioX.Infrastructure;
using RealmStudioX.WPF.ViewModels.Infrastructure;
using RealmStudioX.WPF.ViewModels.Main;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace RealmStudioX.WPF.ViewModels.Controls
{
    public class NameGenConfigViewModel : ViewModelBase
    {
        const int NumberOfNamesToGenerate = 10;
        const int NumberOfNamesToKeep = 30;

        private readonly MainWindowViewModel viewModel;

        public ObservableCollection<string> GeneratedNames { get; } = [];

        private readonly RelayCommand _applyGeneratedNameCommand;
        private readonly RelayCommand _copyGeneratedNamesCommand;

        public NameGenConfigViewModel(MainWindowViewModel viewModel)
        {
            this.viewModel = viewModel;

            _applyGeneratedNameCommand =
                new RelayCommand(
                    execute: ApplyGeneratedName,
                    canExecute: CanApplyGeneratedName);

            _copyGeneratedNamesCommand =
                new RelayCommand(
                    execute: CopyGeneratedNames,
                    canExecute: CanCopyGeneratedNames);

            GeneratedNames.CollectionChanged += GeneratedNames_CollectionChanged;
        }

        private void GeneratedNames_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _copyGeneratedNamesCommand.RaiseCanExecuteChanged();
        }

        public ICommand ApplyGeneratedNameCommand => _applyGeneratedNameCommand;

        public ICommand CopyGeneratedNamesCommand => _copyGeneratedNamesCommand;

        private void ApplyGeneratedName()
        {
            if (string.IsNullOrWhiteSpace(SelectedGeneratedName))
            {
                return;
            }

            viewModel.LabelsViewModel.LabelText = SelectedGeneratedName;
        }

        private bool CanApplyGeneratedName()
        {
            return !string.IsNullOrWhiteSpace(SelectedGeneratedName);
        }

        private void CopyGeneratedNames()
        {
            string text;

            if (!string.IsNullOrWhiteSpace(SelectedGeneratedName))
            {
                text = SelectedGeneratedName;
            }
            else
            {
                text = string.Join(Environment.NewLine, GeneratedNames);
            }

            Clipboard.SetText(text);
        }

        private bool CanCopyGeneratedNames()
        {
            return GeneratedNames.Count > 0;
        }

        public List<NameGenerator> NameGenerators {
            get
            {
                return AssetManager.NameGenerators;
            }
        }

        public List<NameBase> NameBases
        {
            get
            {
                return AssetManager.NameBases;
            }
        }

        public List<NameBaseLanguage> NameLanguages
        {
            get
            {
                return AssetManager.NameLanguages;
            }                
        }

        private bool _allNameGeneratorsSelected = true;

        public bool AllNameGeneratorsSelected
        {
            get => _allNameGeneratorsSelected;

            set
            {
                if (SetProperty(ref _allNameGeneratorsSelected, value))
                {
                    foreach (var generator in NameGenerators)
                    {
                        generator.IsSelected = value;
                    }

                    OnPropertyChanged(nameof(NameGenerators));
                }
            }
        }

        private bool _allNameBasesSelected = true;

        public bool AllNameBasesSelected
        {
            get => _allNameBasesSelected;

            set
            {
                if (SetProperty(ref _allNameBasesSelected, value))
                {
                    foreach (var namebase in NameBases)
                    {
                        namebase.IsNameBaseSelected = value;
                    }

                    OnPropertyChanged(nameof(NameBases));
                }
            }
        }

        private bool _allLanguagesSelected = true;

        public bool AllLanguagesSelected
        {
            get => _allLanguagesSelected;

            set
            {
                if (SetProperty(ref _allLanguagesSelected, value))
                {
                    foreach (var language in NameLanguages)
                    {
                        language.IsLanguageSelected = value;
                    }

                    OnPropertyChanged(nameof(NameLanguages));
                }
            }
        }

        private string? _selectedGeneratedName;

        public string? SelectedGeneratedName
        {
            get => _selectedGeneratedName;
            set
            {
                if (SetProperty(ref _selectedGeneratedName, value))
                {
                    _applyGeneratedNameCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand ClearNamesCommand => new RelayCommand(() =>
        {
            GeneratedNames.Clear();
        });

        public ICommand GenerateNamesCommand => new RelayCommand(() =>
        {
            List<INameGenerator> generators = GetSelectedNameGenerators();

            if (generators.Count > 0)
            {
                int generatedNameCount = 0;

                int guardCount = 0;
                int maxTries = 100;

                while (generatedNameCount < NumberOfNamesToGenerate && guardCount < maxTries)
                {
                    guardCount++;
                    string name = NameManager.GenerateRandomPlaceName(generators);

                    if (!string.IsNullOrEmpty(name))
                    {
                        generatedNameCount++;
                        GeneratedNames.Add(name);

                        if (GeneratedNames.Count > NumberOfNamesToKeep)
                        {
                            GeneratedNames.RemoveAt(0);
                        }
                    }
                }
            }
        });

        public List<INameGenerator> GetSelectedNameGenerators()
        {
            List<INameGenerator> generators = [];

            foreach (NameGenerator generator in NameGenerators)
            {
                if (generator.IsSelected)
                {
                    generators.Add(generator);
                }
            }

            foreach (NameBase nameBase in NameBases)
            {
                if (nameBase.IsNameBaseSelected)
                {
                    generators.Add(nameBase);

                    foreach (NameBaseLanguage language in nameBase.Languages)
                    {
                        foreach (NameBaseLanguage l in NameLanguages)
                        {
                            if (l.Language == language.Language && l.IsLanguageSelected)
                            {
                                if (!generators.Contains(l))
                                {
                                    generators.Add(l);
                                }
                            }
                        }
                    }
                }
            }

            foreach (NameBaseLanguage l in NameLanguages)
            {
                if (l.IsLanguageSelected)
                {
                    if (!generators.Contains(l))
                    {
                        generators.Add(l);
                    }
                }
            }

            return generators;
        }
    }
}
