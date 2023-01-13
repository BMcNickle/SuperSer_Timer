using System;
using System.IO;
using Xamarin.Forms;

namespace SuperSer_Timer
{
    public partial class MainPage : ContentPage
    {
        //Set up variables needed at class level
        readonly string fileName = "StatusFile.txt";
        int fullTime = 0;
        int currentTime = 0;
        char mode = 'E';
        int powerStatus = 0;
        string oldTimeStamp = "";

        public MainPage()
        {
            InitializeComponent();

            SelectMode();
        }

        /*
         * Method checks if the status files exists
         * -- If the file is empty or doesn't exist a new calibration is
         * started, creating the file at the same time
         * -- If the file is empty, it is read to get the current status
         * of the gas heater and the app is updated to reflect it
         */
        private void SelectMode()
        {
            var _statusFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), fileName);

            if (_statusFile == null || !File.Exists(_statusFile))
            {
                NewCalibration();
            }
            else
            {
                ReadStatusFile();
                FixButtons(powerStatus);

                switch (mode)
                {
                    case 'C':
                        ChangeMode("Calibration");
                        break;
                    case 'R':
                        ChangeMode("Run");
                        break;
                    default:
                        ChangeMode("ERROR");
                        break;
                }
            }
            return;
        }

        /*
         * Method to read the Status File and update variable values accordingly
         */
        private void ReadStatusFile()
        {
            var _statusFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), fileName);

            using (var _reader = new StreamReader(_statusFile, true))
            {
                string fileLine;
                int lineRead = 0;
                while ((fileLine = _reader.ReadLine()) != null)
                {
                    switch (lineRead)
                    {
                        case 0:
                            fullTime = Int32.Parse(fileLine);
                            break;
                        case 1:
                            currentTime = Int32.Parse(fileLine);
                            break;
                        case 2:
                            mode = fileLine[0];
                            break;
                        case 3:
                            powerStatus = Int32.Parse(fileLine);
                            break;
                        case 4:
                            oldTimeStamp = fileLine;
                            break;
                    }
                    lineRead++;
                }
            }

            UpdateGuage();

            return;
        }

        /*
         * Method sets background colours, label texts, control visiblity statuses
         * and other settings depending on current mode (Calibration, Run or ERROR)
         */
        public void ChangeMode(string _mode)
        {
            if (_mode == "Calibration")
            {
                Banner.BackgroundColor = Color.DarkCyan;

                Off.BackgroundColor = Color.DarkCyan;
                One.BackgroundColor = Color.DarkCyan;
                Two.BackgroundColor = Color.DarkCyan;
                Three.BackgroundColor = Color.DarkCyan;

                CalibrationSlider.BackgroundColor = Color.DarkCyan;

                FuelGuage.IsVisible = false;
                FuelGuage.Progress = 0;
                FuelLabel.IsVisible = false;

                SliderLabel.Text = "Slide to engage Run Mode";
            }
            else if (_mode == "Run")
            {
                Banner.BackgroundColor = Color.DarkRed;
                Off.BackgroundColor = Color.DarkRed;
                One.BackgroundColor = Color.DarkRed;
                Two.BackgroundColor = Color.DarkRed;
                Three.BackgroundColor = Color.DarkRed;
                CalibrationSlider.BackgroundColor = Color.DarkRed;

                FuelGuage.IsVisible = true;
                FuelLabel.IsVisible = true;

                UpdateGuage();

                SliderLabel.Text = "Slide to engage Calibration Mode";
            }
            else
            {
                Banner.BackgroundColor = Color.Gray;
                Off.BackgroundColor = Color.Gray;
                One.BackgroundColor = Color.Gray;
                Two.BackgroundColor = Color.Gray;
                Three.BackgroundColor = Color.Gray;
                CalibrationSlider.BackgroundColor = Color.Gray;

                FuelGuage.Progress = 0;
                FuelGuage.BackgroundColor = Color.Gray;
                FuelGuage.IsVisible = true;
                FuelLabel.IsVisible = true;

                SliderLabel.Text = "ERROR";
                ModeLabel.Text = "ERROR";


                Off.IsEnabled = false;
                One.IsEnabled = false;
                Two.IsEnabled = false;
                Three.IsEnabled = false;

                DeleteFile();
                SelectMode();

                return;
            }

            ModeLabel.Text = _mode + " Mode";
            return;
        }

        /*
         * Method to update fuel guage to show how much fuel is left
         * 5 stages of fuel guage colour getting darker as gas is emptied
         */
        private void UpdateGuage()
        {
            if (FuelGuage.IsVisible == true)
            {
                FuelGuage.Progress = (double) currentTime / fullTime;

                if (FuelGuage.Progress > 0.45)
                {
                    FuelGuage.BackgroundColor = Color.Green;
                }
                else if (FuelGuage.Progress > 0.25)
                {
                    FuelGuage.BackgroundColor = Color.YellowGreen;
                }
                else if (FuelGuage.Progress > 0.15)
                {
                    FuelGuage.BackgroundColor = Color.OrangeRed;
                }
                else if (FuelGuage.Progress > 0.10)
                {
                    FuelGuage.BackgroundColor = Color.Red;
                }
                else
                {
                    FuelGuage.BackgroundColor = Color.DarkRed;
                }
            }
            return;
        }

        //Delete current status file - only happens when a fatal error occurs
        public void DeleteFile()
        {
            var _statusFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), fileName);

            DisplayAlert("ERROR", "A fatal error has occured.\nAll saved progress has been lossed.", "OK");

            if (File.Exists(_statusFile))
            {
                File.Delete(_statusFile);
            }
        }

        /*
         * Method for switching between Calibration mode and Run mode
         * -- Slider cannot be clicked and must be draged
         * -- Method is only called upon completion of the drag and uses the value of 
         * the finishing point of the drag must be above 95 out of 100
         */
        public async void Calibration_Toggled(object sender, EventArgs e)
        {
            bool answer;

            if (CalibrationSlider.Value > 95)
            {
                //Change to Run mode if currently in Calibration mode - displays warning
                if (mode == 'C')
                {
                    answer = await DisplayAlert("WARNING!", "Enganging 'Run Mode'.\n\n" +
                        "Only do this when you are changing to a new gas cylinder.\n\n" +
                        "THIS ACTION CAN NOT BE UNDONE!\n\n" +
                        "Are you sure you wish to continue?", "Yes", "No");
                    if (answer)
                    {
                        //set power to off and update buttons to reflect it
                        FixButtons(0);
                        ChangePower(0);

                        currentTime = fullTime;
                        mode = 'R';
                        UpdateFile();
                        ChangeMode("Run");
                    }
                }
                //Change to Calibration mode if currently in Run mode - displays warning
                else
                {
                    answer = await DisplayAlert("WARNING!", "Enganging 'Calibration Mode' deletes tank information.\n\n" +
                        "Only do this when you are changing to a new gas cylinder.\n\n" +
                        "THIS ACTION CAN NOT BE UNDONE!\n\n" +
                        "Are you sure you wish to continue?", "Yes", "No");

                    if (answer)
                    {
                        //Start a new clibration
                        NewCalibration();
                        ChangeMode("Calibration");
                    }
                }
            }

            //Returns slide to left regardless of what else happens
            CalibrationSlider.Value = 0;
        }

        /*
         * The following methods are for when the buttons are pressed
         * -- Disable clicked button & enable the rest
         * -- Change powerStatus method call for the button clicked
         */
        public void Off_Clicked(object sender, EventArgs e)
        {
            FixButtons(0);
            ChangePower(0);
        }
        
        public void One_Clicked(object sender, EventArgs e)
        {
            FixButtons(1);
            ChangePower(1);
        }

        public void Two_Clicked(object sender, EventArgs e)
        {
            FixButtons(2);
            ChangePower(2);
        }

        public void Three_Clicked(object sender, EventArgs e)
        {
            FixButtons(3);
            ChangePower(3);
        }

        /*
         * Method causes the buttons to work like radio buttons
         * So the button that is pressed (current powerStatus) is disabled and the others are enabled
         */
        private void FixButtons(int buttonClicked)
        {
            Off.IsEnabled = true;
            One.IsEnabled = true;
            Two.IsEnabled = true;
            Three.IsEnabled = true;

            switch (buttonClicked)
            {
                case 0:
                    Off.IsEnabled = false;
                    break;
                case 1:
                    One.IsEnabled = false;
                    break;
                case 2:
                    Two.IsEnabled = false;
                    break;
                case 3:
                    Three.IsEnabled = false;
                    break;
            }
            return;
        }

        /*
         * Method updates the value of a full tank in Calibration mode or decreases
         * the value of remaining gas if in Run mode by the number of seconds it has been 
         * on that power setting multiplied by the power setting itself
         */
        private void ChangePower(int buttonPressed)
        {
            if (powerStatus != 0)
            {
                TimeSpan duration = DateTime.Now - DateTime.Parse(oldTimeStamp);

                if (mode == 'C')
                {
                    fullTime += (int) duration.TotalSeconds * powerStatus;
                }
                else if (mode == 'R')
                {
                    currentTime -= (int) duration.TotalSeconds * powerStatus;
                }
                else
                {
                    ChangeMode("ERROR");
                }
            }

            //Changes the current powerStatus to match the button that has just been pressed
            powerStatus = buttonPressed;
            oldTimeStamp = DateTime.Now.ToString();

            //Saves the changes made to file and then updates the fuel guage
            UpdateFile();
            UpdateGuage();
            return;
        }

        //Method resets the saved tank details, changes screen to calibration mode then updates the saved file
        private void NewCalibration()
        {
            fullTime = 0;
            currentTime = 0;
            mode = 'C';
            powerStatus = 0;
            oldTimeStamp = DateTime.Now.ToString();

            ChangeMode("Calibration");
            FixButtons(0);

            UpdateFile();

            return;
        }

        //This mehod will update the status file with current status of the gas heater
        private void UpdateFile()
        {
            var _statusFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), fileName);
            
            string writeToFile = fullTime.ToString() + Environment.NewLine +
                currentTime.ToString() + Environment.NewLine +
                mode.ToString() + Environment.NewLine +
                powerStatus.ToString() + Environment.NewLine +
                DateTime.Now.ToString();

            File.WriteAllText(_statusFile, writeToFile);

            return;
        }
    }
}
