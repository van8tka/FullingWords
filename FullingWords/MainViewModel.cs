using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.UI.ViewManagement;
using FullingWords.Annotations;

namespace FullingWords
{
   public class MainViewModel:INotifyPropertyChanged
   {
       private StorageFolder _storage;

        public MainViewModel()
        {
            _storage = ApplicationData.Current.LocalFolder;
            FilesList =  GetFilesList().Result;
            WordsList = new ObservableCollection<ItemModel>();
            CreateFileCommand = new FullingWordsCommand(CreateFile);
            RemoveFileCommand = new FullingWordsCommand(RemoveFile);
            LoadFileCommand = new FullingWordsCommand(LoadWordsList);
            RemoveWordCommand = new FullingWordsCommand(RemoveWord);
            AddWordCommand = new FullingWordsCommand(AddWordToFile);
        }

        public ICommand CreateFileCommand { get; set; }
        public ICommand RemoveFileCommand { get; set; }
        public ICommand LoadFileCommand { get; set; }
        public ICommand RemoveWordCommand { get; set; }
        public ICommand AddWordCommand { get; set; }

       /// <summary>
       /// имя нового файла
       /// </summary>
       private string _newFile;
       public string NewFileName
        {
           get => _newFile;
           set
           {
               if (!string.IsNullOrEmpty(value) && !value.EndsWith(".xml"))
                   value += ".xml";
               _newFile = value;            
               OnPropertyChanged(nameof(NewFileName));
           }
       }       
        /// <summary>
        /// новое слово
        /// </summary>
        private string _newWord;
        public string NewWord
        {
            get => _newWord;
            set
            {
               _newWord = value;
                if(!string.IsNullOrEmpty(value))
                    CountWordOnChar = value[0].ToString();
                CheckExistWord( value );
                OnPropertyChanged(nameof(NewWord));
            }
        }
        /// <summary>
        ///количество слов на вводимую букву
       /// </summary>
        private string _countWordOnChar;
       public string CountWordOnChar
        {
           get => _countWordOnChar;
           set
           {
             int count = WordsList.Where(x => x.Name.StartsWith(value))?.Count()??0;
             _countWordOnChar = "Кол-во слов на букву "+value.ToUpper()+" = "+count;              
             OnPropertyChanged(nameof(CountWordOnChar));
           }
       }




        /// <summary>
        /// если слово есть выводим сообщение
        /// </summary>
        private bool _isExistWord;
        public bool IsExistWord
        {
            get => _isExistWord;
            set
            {
                _isExistWord = value;
                OnPropertyChanged(nameof(IsExistWord));
            }
        }
        /// <summary>
        /// список файлов
        /// </summary>
        private ObservableCollection<ItemModel> _filesList;
        public ObservableCollection<ItemModel> FilesList
        {
            get => _filesList;
            set
            {
                _filesList = value;
                OnPropertyChanged(nameof(FilesList));
            }
        }
        /// <summary>
        /// список слов файла
        /// </summary>
        private ObservableCollection<ItemModel> _wordsList;
        public ObservableCollection<ItemModel> WordsList
        {
            get => _wordsList;
            set
            {
                _wordsList = value;
                OnPropertyChanged(nameof(WordsList));
            }
        }

       
        /// <summary>
        /// выбранный файл
        /// </summary>
        private ItemModel _selectedFile;
        public ItemModel SelectedFile
        {
            get => _selectedFile;
            set
            {
                _selectedFile = value;                
                OnPropertyChanged(nameof(SelectedFile));
            }
        }
        /// <summary>
        /// выбранное слово
        /// </summary>
        private ItemModel _selectedWord;
        public ItemModel SelectedWord
        {
            get => _selectedWord;
            set
            {
                _selectedWord = value;               
                OnPropertyChanged(nameof(SelectedWord));
            }
        }
       /// <summary>
       /// количество слов
       /// </summary>
       private int _countWords;
       public int CountWords
       {
           get => _countWords;
           set
           {
               _countWords = value;
               OnPropertyChanged(nameof(CountWords));
           }
       }

        private string _loadedFilePath { get; set; }

        private void AddWordToFile()
        {
            try
            {
                if (!string.IsNullOrEmpty(_loadedFilePath) && !string.IsNullOrEmpty(NewWord))
                {
                    var xdoc = XDocument.Load(_loadedFilePath);                    
                    xdoc.Element("Root").Add(new XElement("Item", NewWord));
                    SaveFile(xdoc, _loadedFilePath);
                    WordsList.Insert(0,new ItemModel(){Name = NewWord});
                    
                    CountWords += 1;
                    CountWordOnChar = NewWord[0].ToString();
                    NewWord = String.Empty;
                }
            }
            catch (Exception e)
            {                
                throw;
            }         
        }

        private void RemoveWord()
        {
            try
            {
                if (!string.IsNullOrEmpty(_loadedFilePath) && SelectedWord != null)
                {
                    var xdoc = XDocument.Load(_loadedFilePath);
                    xdoc.Element("Root").Elements("Item").Where(x => x.Value.Equals(SelectedWord.Name, StringComparison.OrdinalIgnoreCase))?.Remove();
                    SaveFile(xdoc, _loadedFilePath);
                    WordsList.Remove(SelectedWord);
                    CountWords -= 1;
                }
            }
            catch (Exception e)
            {              
                throw;
            }
          
        }

        private async void RemoveFile()
        {
            if (SelectedFile != null)
            {
               var file = await _storage.GetFileAsync(SelectedFile.Name);
               await file.DeleteAsync(StorageDeleteOption.Default);
                FilesList.Remove(SelectedFile);
            }
        }

        private void CreateFile()
        {
            try
            {
                if (!string.IsNullOrEmpty(NewFileName) && !IsExistWord)
                {
                    var path = Path.Combine(_storage.Path, NewFileName);               
                    var xdoc = new XDocument(new XElement("Root"));
                    SaveFile(xdoc, path);
                    FilesList.Add(new ItemModel() { Name = NewFileName });
                    NewFileName = string.Empty;
                }
            }
            catch (Exception e)
            {            
                throw;
            }               
        }


       private void SaveFile(XDocument xdoc, string path)
       {
           using (var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
           {
               xdoc.Save(stream);
           }
        }

        private void LoadWordsList( )
        {
            if (SelectedFile != null)
            {
                try
                {
                    WordsList.Clear();
                    var path = Path.Combine(_storage.Path, SelectedFile.Name);
                    _loadedFilePath = path;
                    var doc = XDocument.Load(path);
                    var root = XElement.Parse(doc.ToString());
                    var items = root.Elements("Item");
                    foreach (var i in items)
                    {
                        WordsList.Add(new ItemModel(){Name = i.Value});
                    }
                    WordsList?.Reverse();
                    CountWords = WordsList.Count;
                }
                catch (Exception e)
                {                
                    throw;
                }
               
            }
        }
        private async Task<ObservableCollection<ItemModel>> GetFilesList()
        {
            var temp = new ObservableCollection<ItemModel>();
            var items = await _storage.GetFilesAsync();
            foreach (var t in items)
            {
                temp.Add(new ItemModel(){Name = t.Name});
            }
            return temp;
        }
        private void CheckExistWord(string item)
        {
            if (WordsList.Any(x=>x.Name.Equals(item, StringComparison.OrdinalIgnoreCase)))
            {
                IsExistWord = true;
            }
            else
            {
                IsExistWord = false;
            }
        }

        /// <summary>
        /// реализация уведомлений
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
