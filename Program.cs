using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;

namespace CleanCode_ExampleTask21_27
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MessegeBox messegeBox = new MessegeBox();
            string databasePath = "Path";

            Sha256Hasher hasher = new Sha256Hasher();

            DatabaseContext databaseContext = new DatabaseContext(databasePath);
            PassportReposetory passportReposetory = new PassportReposetory(databaseContext);
            PassportFinder passportFinder = new PassportFinder(passportReposetory, hasher);

            Menu menu = new Menu(messegeBox);
            PassportPresenter presenter = new PassportPresenter(menu, passportFinder);

            menu.SetPresenter(presenter);
            menu.HandleUserInput();
        }
    }

    public class PassportPresenter : IPresenter
    {
        private readonly IMessegeViewer _menu;
        private readonly PassportFinder _passportFinder;

        public PassportPresenter(Menu menu, PassportFinder passportFinder)
        {
            if (menu == null)
                throw new ArgumentNullException();

            if (passportFinder == null)
                throw new ArgumentNullException();

            _passportFinder = passportFinder;
            _menu = menu;
        }

        public void HandleData(string passportSeries)
        {
            if (string.IsNullOrWhiteSpace(passportSeries))
                throw new ArgumentNullException();

            Passport passport = new Passport(passportSeries);

            bool result = _passportFinder.GetPassportCheckResult(passport).HaveVoted;

            if (result)
                _menu.ShowMessege("По паспорту «" + passport.Series + "» доступ к бюллетеню на дистанционном электронном голосовании ПРЕДОСТАВЛЕН");
            else if (result == false)
                _menu.ShowMessege("По паспорту «" + passport.Series + "» доступ к бюллетеню на дистанционном электронном голосовании НЕ ПРЕДОСТАВЛЯЛСЯ");
            else
                _menu.ShowMessege("Паспорт «" + passport.Series + "» в списке участников дистанционного голосования НЕ НАЙДЕН");
        }
    }

    public class PassportFinder
    {
        private readonly Sha256Hasher _hasher;
        private readonly PassportReposetory _reposetory;

        public PassportFinder(PassportReposetory repository, Sha256Hasher hasher)
        {
            _reposetory = repository;
            _hasher = hasher;
        }

        public PassportCheckResult GetPassportCheckResult(Passport passport)
        {
            string hash = _hasher.ComputeSha256Hash(passport.Series);
            return _reposetory.GetPassportCheckResult(hash);
        }
    }

    public class Passport
    {
        private const string EmptyChar = " ";
        private const int CorrecrLength = 10;

        public Passport(string series)
        {
            if (string.IsNullOrWhiteSpace(series))
                throw new ArgumentNullException();

            Series = Normalize(series);

            if (IsFormatCorrect(Series) == false)
                throw new ArgumentException();
        }

        public string Series { get; private set; }

        private bool IsFormatCorrect(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                throw new ArgumentNullException();

            if (data.Length != CorrecrLength)
                return false;

            return true;
        }

        private string Normalize(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return null;

            string normalizedData = data.Trim().Replace(EmptyChar, string.Empty);

            return normalizedData;
        }
    }

    public class PassportCheckResult
    {
        public PassportCheckResult(bool haveVoted)
        {
            HaveVoted = haveVoted;
        }

        public bool HaveVoted { get; private set; }
    }

    public class Sha256Hasher
    {
        private const string HexFormat = "x2";

        public string ComputeSha256Hash(string rawData)
        {
            if (string.IsNullOrEmpty(rawData))
                throw new ArgumentNullException();

            var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            var builder = new StringBuilder();

            foreach (var @byte in bytes)
                builder.Append(@byte.ToString(HexFormat));

            return builder.ToString();
        }
    }

    public class PassportReposetory
    {
        private readonly DatabaseContext _databaseContext;

        public PassportReposetory(DatabaseContext databaseContext)
        {
            if(databaseContext == null) 
                throw new ArgumentNullException();

            _databaseContext = databaseContext;
        }

        public PassportCheckResult GetPassportCheckResult(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentNullException();

            DataTable dataTable = _databaseContext.GetDataTable(hash);

            if (dataTable.Rows.Count == 0)
                return null;

            bool granted = Convert.ToBoolean(dataTable.Rows[0].ItemArray[1]);

            return new PassportCheckResult(granted);
        }
    }

    public class DatabaseContext
    {
        private readonly string _dbPath;

        public DatabaseContext(string dbPath)
        {
            if(string.IsNullOrWhiteSpace(dbPath))
                throw new ArgumentNullException();

            _dbPath = dbPath;
        }

        public DataTable GetDataTable(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentNullException();

            string textFormat = "select * from passports where num='{0}' limit 1;";
            string connectionFormat = "Data Source=";

            string commandText = string.Format(textFormat, hash);
            string connectionString = string.Format(connectionFormat + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + _dbPath);

            SQLiteConnection connection = new SQLiteConnection(connectionString);
            SQLiteDataAdapter sqLiteDataAdapter = new SQLiteDataAdapter(new SQLiteCommand(commandText, connection));

            connection.Open();

            DataTable dataTable = new DataTable();

            sqLiteDataAdapter.Fill(dataTable);

            return dataTable;
        }
    }

    public class Menu : IMessegeViewer
    {
        private readonly MessegeBox _messegeBox;
        private IPresenter _presenter;

        public Menu(MessegeBox messegeBox)
        {
            if(messegeBox == null)
                throw new ArgumentNullException();

            _messegeBox = messegeBox;
        }

        public void HandleUserInput()
        {
            string input = Console.ReadLine();
            _presenter.HandleData(input);
        }

        public void ShowMessege(string messege)
        {
            if(string.IsNullOrWhiteSpace(messege))
                throw new ArgumentNullException();

            _messegeBox.Show(messege);
        }

        public void SetPresenter(IPresenter presenter)
        {
            if(presenter == null)
                throw new ArgumentNullException();

            _presenter = presenter;
        }
    }

    public class MessegeBox
    {
        public void Show(string messege)
        {
            if(string.IsNullOrWhiteSpace(messege))
                throw new ArgumentNullException();

            throw new NotImplementedException();
        }
    }

    public interface IMessegeViewer
    {
        void ShowMessege(string messege);
        void SetPresenter(IPresenter presenter);
    }

    public interface IPresenter
    {
        void HandleData(string data);
    }
}
