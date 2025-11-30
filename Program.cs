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


        }
    }

    public class PassportService
    {
        private PasportValidator _validator;
        private PassportRepository _repository;
        private Sha256Hasher _shaHasher;

        public PassportService(PasportValidator validator, PassportRepository repository, Sha256Hasher shaHasher)
        {
            if (validator == null)
                throw new ArgumentNullException();

            if (repository == null)
                throw new ArgumentNullException();

            if (shaHasher == null)
                throw new ArgumentNullException();

            _validator = validator;
            _repository = repository;
            _shaHasher = shaHasher;
        }

        public PassportCheckResult CheckPassport(Passport passport)
        {
            if(passport  == null)
                throw new ArgumentNullException();

            string passportData = _validator.Validate(passport);
            string hash = _shaHasher.ComputeSha256Hash(passportData);

            DataTable dataTable = _repository.GetPassportData(hash);

            if (dataTable.Rows.Count == 0)
                return new PassportCheckResult(false, false);

            bool granted = Convert.ToBoolean(dataTable.Rows[0].ItemArray[1]);

            return new PassportCheckResult(true, granted);
        }
    }

    public class Passport
    {
        public Passport(string series)
        {
            if (series == null)
                throw new ArgumentNullException();

            Series = series;
        }

        public string Series { get; private set; }
    }

    public class PassportCheckResult
    {
        public PassportCheckResult(bool exist, bool granted)
        {
            Exist = exist;
            Granted = granted;
        }

        public bool Exist { get; private set; }
        public bool Granted { get; private set; }

    }

    public class PasportValidator
    {
        private const string EmptyChar = " ";
        private const int CorrecrLength = 10;

        public string Validate(Passport passport)
        {
            if (passport == null)
                throw new ArgumentNullException();

            string passportData = Normalize(passport.Series);

            if (IsFormatCorrect(passportData) == false)
                throw new ArgumentException();

            return passportData;
        }

        private bool IsFormatCorrect(string data)
        {
            if (data == null)
                throw new ArgumentNullException();

            if (data.Length != CorrecrLength)
                return false;

            return true;
        }

        private string Normalize(string data)
        {
            if (data == null)
                throw new ArgumentNullException();

            string normalizedData = data.Trim().Replace(EmptyChar, string.Empty);

            if (normalizedData.Length == 0)
                return null;

            return normalizedData;
        }
    }

    public class Sha256Hasher
    {
        private const string HexFormat = "x2";

        public string ComputeSha256Hash(string rawData)
        {
            if (rawData == null)
                throw new ArgumentNullException();

            var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            var builder = new StringBuilder();

            foreach (var @byte in bytes)
                builder.Append(@byte.ToString(HexFormat));

            return builder.ToString();
        }
    }

    public class PassportRepository
    {
        private readonly string _dbPath;

        public PassportRepository(string dbPath)
        {
            if(dbPath == null)
                throw new ArgumentNullException();

            _dbPath = dbPath;
        }

        public DataTable GetPassportData(string hash)
        {
            if(hash == null) 
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

    public class Menu
    {
        public void ShowPassportInfo(PassportService passportService, Passport passport)
        {
            if(passport == null)
                throw new ArgumentNullException();

            if(passportService == null)
                throw new ArgumentNullException();

            PassportCheckResult result = passportService.CheckPassport(passport);

            if (result.Exist == false)
                textResult.Text = "По паспорту «" + passport.Series + "» доступ к бюллетеню на дистанционном электронном голосовании ПРЕДОСТАВЛЕН";
            else if (result.Granted)
                textResult.Text = "По паспорту «" + passport.Series + "» доступ к бюллетеню на дистанционном электронном голосовании НЕ ПРЕДОСТАВЛЯЛСЯ";
            else
                textResult.Text = "Паспорт «" + passport.Series + "» в списке участников дистанционного голосования НЕ НАЙДЕН";
        }
    }
}
