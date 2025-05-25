using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Media;

namespace InsectCatchGame
{
    public partial class MainForm : Form
    {
        private Random random = new Random();
        private int score = 0;
        private int timeLeft;
        private int level = 1;
        private Timer gameTimer;
        private Timer insectTimer;
        private PictureBox insectPicture;
        private Label scoreLabel;
        private Label timeLabel;
        private Label recordLabel;
        private Button level1Button;
        private Button level2Button;
        private Button level3Button;
        private Button endlessButton;
        private Button resetButton;
        private Button backButton;
        private Panel menuPanel;
        private Panel gamePanel;
        private Image backgroundImage;
        private Cursor customCursor;
        private int levelPassScore = 10;
        private bool endlessMode = false;
        private Dictionary<string, InsectType> insectTypes;
        private List<Point> previousLocations = new List<Point>();
        private int endlessRecord = 0;
        private HashSet<int> completedLevels = new HashSet<int>();
        private const string RecordsFile = "Records.txt";
        private SoundPlayer clickSound;
        private SoundPlayer levelCompleteSound;
        private SoundPlayer gameOverSound;
        private SoundPlayer backgroundMusic;

        public MainForm()
        {
            InitializeComponent();
            InitializeInsectTypes();
            LoadRecords();
            LoadResources();
            CreateMenu();
            CreateGamePanel();
            SwitchToMenu();
        }

        private void InitializeInsectTypes()
        {
            insectTypes = new Dictionary<string, InsectType>
            {
                { "cockroach", new InsectType("img/cockroach.png", 1600, 1) },
                { "comar", new InsectType("img/comar.png", 800, 2) },
                { "fly", new InsectType("img/fly.png", 600, 3) }
            };
        }

        private void LoadRecords()
        {
            if (File.Exists(RecordsFile))
            {
                try
                {
                    string[] lines = File.ReadAllLines(RecordsFile);
                    if (lines.Length > 0)
                    {
                        if (int.TryParse(lines[0], out int record))
                            endlessRecord = record;

                        for (int i = 1; i < lines.Length; i++)
                            if (int.TryParse(lines[i], out int level))
                                completedLevels.Add(level);
                    }
                }
                catch
                {
                }
            }
        }

        private void SaveRecords()
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add(endlessRecord.ToString());
                foreach (int level in completedLevels)
                    lines.Add(level.ToString());
                File.WriteAllLines(RecordsFile, lines);
            }
            catch
            {
                MessageBox.Show("Не удалось сохранить рекорды!", "Ошибка");
            }
        }

        private void LoadResources()
        {
            try
            {
                backgroundImage = Image.FromFile("img/fon.png");
                customCursor = CreateCursor("img/cursor.png", 15);
                this.Cursor = customCursor;

                clickSound = new SoundPlayer("sounds/clap.wav");
                levelCompleteSound = new SoundPlayer("sounds/level_complete.wav");
                gameOverSound = new SoundPlayer("sounds/game_over.wav");
                backgroundMusic = new SoundPlayer("sounds/background.wav");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ресурсов: {ex.Message}");
                backgroundImage = new Bitmap(800, 600);
                using (Graphics g = Graphics.FromImage(backgroundImage))
                {
                    g.Clear(Color.LightGreen);
                }
                this.Cursor = Cursors.Default;
            }
        }

        private Cursor CreateCursor(string path, int reduceFactor)
        {
            try
            {
                Bitmap original = new Bitmap(path);
                original.MakeTransparent(Color.White);

                int newWidth = original.Width / reduceFactor;
                int newHeight = original.Height / reduceFactor;

                if (newWidth < 16) newWidth = 16;
                if (newHeight < 16) newHeight = 16;

                Bitmap resized = new Bitmap(newWidth, newHeight);
                using (Graphics g = Graphics.FromImage(resized))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(original, 0, 0, newWidth, newHeight);
                }

                return new Cursor(resized.GetHicon());
            }
            catch
            {
                return Cursors.Default;
            }
        }

        private void InitializeComponent()
        {
            this.ClientSize = new Size(800, 600);
            this.Text = "Ловля насекомых";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackgroundImageLayout = ImageLayout.Stretch;
        }

        private void CreateMenu()
        {
            menuPanel = new Panel
            {
                Size = this.ClientSize,
                BackgroundImage = backgroundImage,
                BackgroundImageLayout = ImageLayout.Stretch
            };

            Label titleLabel = new Label
            {
                Text = "Ловля насекомых",
                Font = new Font("Arial", 24, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(250, 50)
            };

            level1Button = new Button
            {
                Text = "Уровень 1 (таракан)",
                Size = new Size(220, 50),
                Location = new Point(290, 150),
                Font = new Font("Arial", 12),
                Tag = 1,
                BackColor = completedLevels.Contains(1) ? Color.LightGreen : Color.White,
                Cursor = customCursor
            };
            level1Button.Click += LevelButton_Click;

            level2Button = new Button
            {
                Text = "Уровень 2 (комар)",
                Size = new Size(220, 50),
                Location = new Point(290, 210),
                Font = new Font("Arial", 12),
                Tag = 2,
                BackColor = completedLevels.Contains(2) ? Color.LightGreen : Color.White,
                Cursor = customCursor
            };
            level2Button.Click += LevelButton_Click;

            level3Button = new Button
            {
                Text = "Уровень 3 (муха)",
                Size = new Size(220, 50),
                Location = new Point(290, 270),
                Font = new Font("Arial", 12),
                Tag = 3,
                BackColor = completedLevels.Contains(3) ? Color.LightGreen : Color.White,
                Cursor = customCursor
            };
            level3Button.Click += LevelButton_Click;

            endlessButton = new Button
            {
                Text = $"Бесконечный режим (рекорд: {endlessRecord})",
                Size = new Size(220, 50),
                Location = new Point(290, 330),
                Font = new Font("Arial", 12),
                BackColor = Color.White,
                Cursor = customCursor
            };
            endlessButton.Click += EndlessButton_Click;

            resetButton = new Button
            {
                Text = "Сброс прогресса",
                Size = new Size(220, 40),
                Location = new Point(290, 400),
                Font = new Font("Arial", 10),
                BackColor = Color.White,
                Cursor = customCursor
            };
            resetButton.Click += ResetButton_Click;

            menuPanel.Controls.Add(titleLabel);
            menuPanel.Controls.Add(level1Button);
            menuPanel.Controls.Add(level2Button);
            menuPanel.Controls.Add(level3Button);
            menuPanel.Controls.Add(endlessButton);
            menuPanel.Controls.Add(resetButton);

            this.Controls.Add(menuPanel);
        }

        private void CreateGamePanel()
        {
            gamePanel = new Panel
            {
                Size = this.ClientSize,
                BackgroundImage = backgroundImage,
                BackgroundImageLayout = ImageLayout.Stretch,
                Visible = false
            };

            backButton = new Button
            {
                Text = "В меню",
                Size = new Size(100, 30),
                Location = new Point(10, 10),
                Font = new Font("Arial", 10),
                BackColor = Color.White,
                Cursor = customCursor
            };
            backButton.Click += BackButton_Click;

            scoreLabel = new Label
            {
                Text = "Очки: 0",
                Font = new Font("Arial", 14),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(350, 10)
            };

            timeLabel = new Label
            {
                Text = "Время: 30",
                Font = new Font("Arial", 14),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(350, 40)
            };

            recordLabel = new Label
            {
                Text = endlessMode ? $"Рекорд: {endlessRecord}" : "",
                Font = new Font("Arial", 14),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(350, 70)
            };

            insectPicture = new PictureBox
            {
                Size = new Size(50, 50),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Location = GetRandomLocation(),
                BackColor = Color.Transparent
            };
            insectPicture.Click += InsectPicture_Click;

            gamePanel.Controls.Add(backButton);
            gamePanel.Controls.Add(scoreLabel);
            gamePanel.Controls.Add(timeLabel);
            gamePanel.Controls.Add(recordLabel);
            gamePanel.Controls.Add(insectPicture);

            this.Controls.Add(gamePanel);
        }

        private void SwitchToMenu()
        {
            menuPanel.Visible = true;
            gamePanel.Visible = false;
            StopAllTimers();
            UpdateMenuButtons();

            PlayBackgroundMusic();
        }

        private void PlayBackgroundMusic()
        {
            try
            {
                if (backgroundMusic != null)
                    backgroundMusic.PlayLooping();
            }
            catch { }
        }

        private void StopBackgroundMusic()
        {
            try
            {
                if (backgroundMusic != null)
                    backgroundMusic.Stop();
            }
            catch { }
        }

        private void StopAllTimers()
        {
            if (gameTimer != null)
            {
                gameTimer.Stop();
                gameTimer.Dispose();
                gameTimer = null;
            }
            if (insectTimer != null)
            {
                insectTimer.Stop();
                insectTimer.Dispose();
                insectTimer = null;
            }
        }

        private void UpdateMenuButtons()
        {
            level1Button.BackColor = completedLevels.Contains(1) ? Color.LightGreen : Color.White;
            level2Button.BackColor = completedLevels.Contains(2) ? Color.LightGreen : Color.White;
            level3Button.BackColor = completedLevels.Contains(3) ? Color.LightGreen : Color.White;
            endlessButton.Text = $"Бесконечный режим (рекорд: {endlessRecord})";
        }

        private void SwitchToGame()
        {
            StopBackgroundMusic();
            menuPanel.Visible = false;
            gamePanel.Visible = true;
            score = 0;
            scoreLabel.Text = "Очки: 0";
            recordLabel.Text = "";
            endlessMode = false;
            previousLocations.Clear();

            timeLeft = 20;
            timeLabel.Text = $"Время: {timeLeft}";

            string insectKey = level == 1 ? "cockroach" : (level == 2 ? "comar" : "fly");
            SetInsectImage(insectKey);

            gameTimer = new Timer { Interval = 1000 };
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();

            insectTimer = new Timer { Interval = insectTypes[insectKey].Speed };
            insectTimer.Tick += (s, e) => MoveInsect();
            insectTimer.Start();
        }

        private void StartEndlessMode()
        {
            StopBackgroundMusic();
            menuPanel.Visible = false;
            gamePanel.Visible = true;
            score = 0;
            scoreLabel.Text = "Очки: 0";
            recordLabel.Text = $"Рекорд: {endlessRecord}";
            endlessMode = true;
            timeLeft = 15;
            timeLabel.Text = $"Время: {timeLeft}";
            previousLocations.Clear();

            gameTimer = new Timer { Interval = 1000 };
            gameTimer.Tick += EndlessTimer_Tick;
            gameTimer.Start();

            insectTimer = new Timer { Interval = 1000 };
            insectTimer.Tick += (s, e) => MoveInsect();
            insectTimer.Start();

            RandomizeInsect();
        }

        private Point GetUniqueRandomLocation()
        {
            Point newLocation;
            int attempts = 0;
            int maxAttempts = 100;

            do
            {
                newLocation = GetRandomLocation();
                attempts++;

                if (attempts >= maxAttempts)
                {
                    previousLocations.Clear();
                    return newLocation;
                }
            }
            while (previousLocations.Contains(newLocation));

            previousLocations.Add(newLocation);

            if (previousLocations.Count > 10)
                previousLocations.RemoveAt(0);

            return newLocation;
        }

        private void MoveInsect()
        {
            insectPicture.Location = GetUniqueRandomLocation();

            if (endlessMode)
                RandomizeInsect();
        }

        private void RandomizeInsect()
        {
            int rand = random.Next(100);
            string insectKey;

            if (rand < 40) insectKey = "cockroach";
            else if (rand < 80) insectKey = "comar";
            else insectKey = "fly";

            SetInsectImage(insectKey);
            insectTimer.Interval = insectTypes[insectKey].Speed;
        }

        private void SetInsectImage(string insectKey)
        {
            try
            {
                insectPicture.Image = Image.FromFile(insectTypes[insectKey].ImagePath);
                insectPicture.BackColor = Color.Transparent;
            }
            catch
            {
                insectPicture.Image = null;
                MessageBox.Show($"Изображение {insectKey}.png не найдено!", "Ошибка");
            }
        }

        private void LevelButton_Click(object sender, EventArgs e)
        {
            level = (int)((Button)sender).Tag;
            SwitchToGame();
        }

        private void EndlessButton_Click(object sender, EventArgs e)
        {
            StartEndlessMode();
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите сбросить весь прогресс и рекорды?",
                                       "Подтверждение сброса",
                                       MessageBoxButtons.YesNo,
                                       MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                endlessRecord = 0;
                completedLevels.Clear();
                SaveRecords();
                UpdateMenuButtons();
            }
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            SwitchToMenu();
        }

        private void InsectPicture_Click(object sender, EventArgs e)
        {
            PlaySound(clickSound);
            score++;
            scoreLabel.Text = $"Очки: {score}";

            if (endlessMode)
            {
                string currentInsect = GetCurrentInsectType();
                timeLeft += insectTypes[currentInsect].TimeBonus;
                timeLabel.Text = $"Время: {timeLeft}";

                if (score > endlessRecord)
                {
                    endlessRecord = score;
                    recordLabel.Text = $"Рекорд: {endlessRecord}";
                    SaveRecords();
                }

                RandomizeInsect();
            }
            else if (score >= levelPassScore)
            {
                gameTimer.Stop();

                if (!completedLevels.Contains(level))
                {
                    completedLevels.Add(level);
                    SaveRecords();
                }
                PlaySound(levelCompleteSound);
                MessageBox.Show($"Уровень {level} пройден! Поздравляем!", "Успех");
                SwitchToMenu();
            }
            else
            {
                MoveInsect();
            }
        }

        private string GetCurrentInsectType()
        {
            foreach (var insect in insectTypes)
            {
                if (insectPicture.Image != null &&
                    insectPicture.ImageLocation != null &&
                    insectPicture.ImageLocation.Contains(insect.Key))
                    return insect.Key;
            }
            return "cockroach";
        }

        private Point GetRandomLocation()
        {
            if (insectPicture == null || gamePanel == null)
                return new Point(20, 70);

            int maxX = gamePanel.Width - insectPicture.Width - 20;
            int maxY = gamePanel.Height - insectPicture.Height - 20;

            return new Point(
                random.Next(20, maxX),
                random.Next(70, maxY)
            );
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            timeLeft--;
            timeLabel.Text = $"Время: {timeLeft}";

            if (timeLeft <= 0)
            {
                gameTimer.Stop();
                PlaySound(gameOverSound);
                MessageBox.Show($"Игра окончена! Ваш счет: {score}", "Результат");
                SwitchToMenu();
            }
        }

        private void EndlessTimer_Tick(object sender, EventArgs e)
        {
            timeLeft--;
            timeLabel.Text = $"Время: {timeLeft}";

            if (timeLeft <= 0)
            {
                gameTimer.Stop();
                PlaySound(gameOverSound);
                MessageBox.Show($"Бесконечный режим окончен! Ваш счет: {score}", "Результат");
                SwitchToMenu();
            }
        }

        private void PlaySound(SoundPlayer sound)
        {
            try
            {
                if (sound != null)
                    sound.Play();
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            StopBackgroundMusic();


            clickSound?.Dispose();
            levelCompleteSound?.Dispose();
            gameOverSound?.Dispose();
            backgroundMusic?.Dispose();
        }
    }

    public class InsectType
    {
        public string ImagePath { get; }
        public int Speed { get; }
        public int TimeBonus { get; }

        public InsectType(string imagePath, int speed, int timeBonus)
        {
            ImagePath = imagePath;
            Speed = speed;
            TimeBonus = timeBonus;
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}