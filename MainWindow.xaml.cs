using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using GlmSharp;

namespace Manipulator
{
    public partial class MainWindow
    {
        private readonly Eng eng = new Eng();

        readonly ManipulatorModel manipulatorModel = new ManipulatorModel();

        private readonly Triangle[] axes;
        private readonly Triangle[] square;
        private Model teapot;

        private readonly DispatcherTimer timer;
        private bool isSimulationRunning;
        double prevTime;

        //mouse state
        private bool isMousePressed;
        private float prevX;
        private float prevY;
        //scene rotation angles
        private float ax = (float)Math.PI/5;
        private float ay = (float)Math.PI/4;
        //camera distance from origin
        private float distance = 20f;
        //view transformation
        private Transform viewTransform;


        public MainWindow()
        {
            InitializeComponent();
            //Add engine view as children to the second column of the grid
            grid.Children.Add(eng);
            Grid.SetColumn(eng, 1);

            isSimulationRunning = tbPlayPause.IsChecked == true;
            UpdatePlayPauseButtonText();

            //timer setup
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(0)
            };
            timer.Tick += draw;
            timer.Start();

            //mouse event handlers
            eng.MouseMove += eng_MouseMove;
            eng.MouseDown += eng_MouseDown;
            eng.MouseUp += eng_MouseUp;
            eng.MouseWheel += EngOnMouseWheel;

            //create meshes
            axes = new Triangle[6];
            axes[0] = new Triangle(new vec3(0, 0.1f, 0), new vec3(0, -0.1f, 0), new vec3(1f, 0.1f, 0), new vec4(1, 0, 0, 1));//x
            axes[1] = new Triangle(new vec3(-0.1f, 0, 0), new vec3(0.1f, 0, 0), new vec3(0, 1f, 0), new vec4(0, 1, 0, 1));//y
            axes[2] = new Triangle(new vec3(-0.1f, 0, 0f), new vec3(0.1f, 0, 0f), new vec3(0, 0, 1f), new vec4(0, 0, 1, 1));//z

            axes[3] = new Triangle(new vec3(0, 0, 0.1f), new vec3(0, 0, -0.1f), new vec3(1f, 0.1f, 0), new vec4(1, 0, 0, 1));//x
            axes[4] = new Triangle(new vec3(0, 0, -0.1f), new vec3(0, 0, 0.1f), new vec3(0, 1f, 0), new vec4(0, 1, 0, 1));//y
            axes[5] = new Triangle(new vec3(0, -0.1f, 0f), new vec3(0, 0.1f, 0f), new vec3(0, 0, 1f), new vec4(0, 0, 1, 1));//z

            square = new Triangle[2];
            square[0] = new Triangle(new vec3(-1, -1, 0), new vec3(1, -1, 0), new vec3(1, 1, 0), new vec4(0.7f, 0.2f, 0.2f, 1));
            square[1] = new Triangle(new vec3(-1, -1, 0), new vec3(1, 1, 0), new vec3(-1, 1, 0), new vec4(0.7f, 0.2f, 0.2f, 1));
            Eng.CalculateNormals(square);

            LoadTeapot();

            //setup light source
            Light light = new Light
            {
                intensity = 0,
                position = new vec3(-2f,10,2f)
            };
            eng.LightSources.Add(light);
            SyncLightPositionUi();

            prevTime = DateTime.Now.Ticks / (double)TimeSpan.TicksPerSecond;

            //reset manipulator state to default
            manipulatorModel.Reset();
        }

        private void LoadTeapot()
        {
            const string resourceName = "Manipulator.teapot.obj";
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream teapotStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (teapotStream == null)
                    throw new InvalidOperationException("Resource not found: " + resourceName);

                using (StreamReader teapotReader = new StreamReader(teapotStream))
                {
                    teapot = new Model(teapotReader,new vec4(0.1f,0.3f,1f,1f),cbFakeNormals.IsChecked==true);
                }
            }
        }

        private void draw(object sender, EventArgs e)
        {
            double time = DateTime.Now.Ticks / (double)TimeSpan.TicksPerSecond;
            float dt= (float)(time - prevTime);
            float animationDt = dt*(float)sliderSpeed.Value;

            if(!isSimulationRunning)
                animationDt = 0;


            MoveAim(dt);

            updateViewTransform();
            eng.camera.viewTransform = viewTransform;

            //update manipulator state
            manipulatorModel.InverseKinematics(animationDt*0.5f);
            syncParams();

            //draw axes
            Face face = eng.showFace;
            eng.showFace = Face.Both;
            bool lighting = eng.enableLighting;
            eng.enableLighting = false;
            eng.Render(axes, new Transform(mat3.Identity, new vec3(0, 0, 0.01f)));
            eng.showFace =face;
            eng.enableLighting= lighting;

            //draw square
            var squareTransform = new Transform(mat3.Identity*5, new vec3(0, 0, -0.1f));
            squareTransform.Rotate(new vec3(1, 0, 0), -(float)Math.PI / 2);
            eng.Render(square, squareTransform);

            //draw manipulator
            manipulatorModel.Render(eng, animationDt);

            //draw object to aim for
            eng.Render(teapot.mesh, new Transform(mat3.Identity*0.04f, manipulatorModel.aim+new vec3(0, -0.04f,0 )));

            //update bitmap image
            eng.Present();

            txtResult.Content = "Hand position: " + manipulatorModel.resPos.ToString();
            lbFrameTime.Content = (dt*1000).ToString("0.0") + " ms"+ " ("+(1/dt).ToString("0.0")+" fps)";
            prevTime = time;
        }

        void updateViewTransform()
        {
            viewTransform = Transform.identity;
            viewTransform.Rotate(new vec3(0, 1, 0), ay);
            viewTransform.Rotate(new vec3(1, 0, 0), ax);
            viewTransform.translation += new vec3(0, 0, -distance);
        }

        void syncParams()
        {
            length1.Value = manipulatorModel.lengths[0];
            length2.Value = manipulatorModel.lengths[1];
            length3.Value = manipulatorModel.lengths[2];
            length4.Value = manipulatorModel.lengths[3];
            length5.Value = manipulatorModel.lengths[4];
            length6.Value = manipulatorModel.lengths[5];
            length7.Value = manipulatorModel.lengths[6];

            UpdateUiLengths();
        }

        //Aim movement with keyboard
        void MoveAim(float dt)
        {
            float speed = 2;

            //Matrix to align movement with camera axes
            var transform = new mat3(
                (float)Math.Cos(-ay), 0, (float)-Math.Sin(-ay),
                0, 0, 0,
                (float)Math.Sin(-ay),  0,(float)Math.Cos(-ay));

            vec3 delta = new vec3(0, 0, 0);

            if (Keyboard.IsKeyDown(Key.W))
                delta -= transform * new vec3(0, 0, 1) * dt * speed;
            if (Keyboard.IsKeyDown(Key.S))
                delta += transform * new vec3(0, 0, 1) * dt * speed;
            if (Keyboard.IsKeyDown(Key.A))
                delta -= transform * new vec3(1, 0, 0) * dt * speed;
            if (Keyboard.IsKeyDown(Key.D))
                delta += transform * new vec3(1, 0, 0) * dt * speed;
            if (Keyboard.IsKeyDown(Key.E))
                delta += new vec3(0, 1, 0) * dt * speed;
            if (Keyboard.IsKeyDown(Key.Q))
                delta -= new vec3(0, 1, 0) * dt * speed;


            if (delta != new vec3(0, 0, 0))
            {
                manipulatorModel.aim += delta;
                aimX.Value+= delta.x;
                aimY.Value+= delta.y;
                aimZ.Value+=delta.z;
            }
        }



        private void eng_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isMousePressed = true;
            prevX = (float)e.GetPosition(eng).X;
            prevY = (float)e.GetPosition(eng).Y;
        }

        private void eng_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isMousePressed = false;
        }

        private void eng_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMousePressed)
            {
                float dx = (float)e.GetPosition(eng).X - prevX;
                float dy = (float)e.GetPosition(eng).Y - prevY;

                ay+=dx * 0.005f;
                ax+=dy * 0.005f;

                if(ax<-(float)Math.PI/2)
                    ax= -(float)Math.PI/2;
                if(ax>(float)Math.PI/2)
                    ax=  (float)Math.PI/2;

                prevX = (float)e.GetPosition(eng).X;
                prevY = (float)e.GetPosition(eng).Y;
            }
        }

        private void EngOnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            distance*= (float)Math.Pow(0.9, e.Delta / 120.0);
        }


        //UI events
        private void SliderSpeed_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (lbSpeed != null)
                lbSpeed.Content ="Simulation speed: "+ sliderSpeed.Value.ToString("0.00");
        }

        private void TbPlayPause_Checked(object sender, RoutedEventArgs e)
        {
            isSimulationRunning = true;
            UpdatePlayPauseButtonText();
        }

        private void TbPlayPause_Unchecked(object sender, RoutedEventArgs e)
        {
            isSimulationRunning = false;
            UpdatePlayPauseButtonText();
        }

        private void UpdatePlayPauseButtonText()
        {
            if (tbPlayPause != null)
                tbPlayPause.Content = isSimulationRunning ? "Pause" : "Play";
        }

        private void ResolutionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(comboResolution.SelectedItem is ComboBoxItem item) || item.Content == null)
                return;

            string[] tokens = item.Content.ToString().Split('x');
            if (tokens.Length != 2)
                return;

            if (int.TryParse(tokens[0], out int width) && int.TryParse(tokens[1], out int height))
                eng.SetExtent(width, height);
        }

        private void btResetClick(object sender, RoutedEventArgs e)
        {
            manipulatorModel.Reset();
        }

        private void CbLight_OnClick(object sender, RoutedEventArgs e)
        {
            eng.enableLighting = cbLight.IsChecked == true;
        }

        private void rbBoth(object sender, RoutedEventArgs e)
        {
            eng.showFace = Face.Both;
        }

        private void rbFront(object sender, RoutedEventArgs e)
        {
            eng.showFace = Face.Front;
        }

        private void rbBack(object sender, RoutedEventArgs e)
        {
            eng.showFace = Face.Back;
        }

        private void backgroundColorUpdate(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            eng.clearColor=new vec4((float)slbgR.Value/255f, (float)slbgG.Value/255f, (float)slbgB.Value/255f, 1);
        }

        private void slAmbientIntensity_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            eng.ambientLight=(float)slAmbientIntensity.Value;
        }

        private void SlLight1Intensity_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(eng.LightSources != null && eng.LightSources.Count>0)
                eng.LightSources[0].intensity = (float)slLight1Intensity.Value;
        }

        private void SlLightPosition_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (eng.LightSources == null || eng.LightSources.Count == 0)
                return;

            eng.LightSources[0].position = new vec3((float)slLightPosX.Value, (float)slLightPosY.Value, (float)slLightPosZ.Value);
        }

        private void SyncLightPositionUi()
        {
            if (eng.LightSources == null || eng.LightSources.Count == 0)
                return;

            vec3 pos = eng.LightSources[0].position;
            if (slLightPosX != null)
                slLightPosX.Value = pos.x;
            if (slLightPosY != null)
                slLightPosY.Value = pos.y;
            if (slLightPosZ != null)
                slLightPosZ.Value = pos.z;
        }

        private void SlSphereDetail_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(cbFakeNormals!=null)
                Sphere.buildSphere((int)slSphereDetail.Value, cbFakeNormals.IsChecked==true);
        }

        private void CbFakeNormalsSphere_OnClick(object sender, RoutedEventArgs e)
        {
            Sphere.buildSphere((int)slSphereDetail.Value, cbFakeNormals.IsChecked==true);
            Cylinder.buildCylinder(10, cbFakeNormals.IsChecked == true);
            LoadTeapot();
        }


        private void Length1_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            manipulatorModel.lengths[0] = (float)length1.Value;
            UpdateUiLengths();
        }

        private void Length2_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            manipulatorModel.lengths[1] = (float)length2.Value;
            UpdateUiLengths();
        }

        private void Length3_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            manipulatorModel.lengths[2] = (float)length3.Value;
            UpdateUiLengths();
        }

        private void Length4_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            manipulatorModel.lengths[3] = (float)length4.Value;
            UpdateUiLengths();
        }

        private void Length5_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            manipulatorModel.lengths[4] = (float)length5.Value;
            UpdateUiLengths();
        }

        private void Length6_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            manipulatorModel.lengths[5] = (float)length6.Value;
            UpdateUiLengths();
        }

        private void Length7_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            manipulatorModel.lengths[6] = (float)length7.Value;
            UpdateUiLengths();
        }

        private void UpdateUiLengths()
        {
            if(txtLength1!=null)
                txtLength1.Content = "l1:\t"+length1.Value.ToString("0.0");
            if(txtLength2!=null)
                txtLength2.Content = "l2:\t"+length2.Value.ToString("0.0");
            if(txtLength3!=null)
                txtLength3.Content = "l3:\t"+length3.Value.ToString("0.0");
            if(txtLength4!=null)
                txtLength4.Content = "l4:\t"+length4.Value.ToString("0.0");
            if(txtLength5!=null)
                txtLength5.Content = "l5:\t"+length5.Value.ToString("0.0");
            if(txtLength6!=null)
                txtLength6.Content = "l6:\t"+length6.Value.ToString("0.0");
            if(txtLength7!=null)
                txtLength7.Content = "l7:\t"+length7.Value.ToString("0.0");
        }

        private void Aim_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (aimX != null)
            {
                manipulatorModel.aim.x = (float)aimX.Value;
                txtAimX.Content = "Aim X:\t" + aimX.Value.ToString("0.000");
            }

            if (aimY != null)
            {
                manipulatorModel.aim.y = (float)aimY.Value;
                txtAimY.Content = "Aim Y:\t" + aimY.Value.ToString("0.000");
            }

            if (aimZ != null)
            {
                manipulatorModel.aim.z = (float)aimZ.Value;
                txtAimZ.Content = "Aim Z:\t" + aimZ.Value.ToString("0.000");
            }
            if(manipulatorModel!=null)
                manipulatorModel.InverseKinematics(0);
        }

        private void RandomAim(object sender, RoutedEventArgs e)
        {
            Random rand = new Random();
            vec3 new_pos;

            while (true)
            {
                new_pos = new vec3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                new_pos = new_pos * new vec3(10,5,10) - new vec3(5,0,5);
                float r = new_pos.xz.Length;
                if(r>0.1 && r< 5)
                    break;
            }

            aimX.Value = new_pos.x;
            aimY.Value= new_pos.y;
            aimZ.Value=new_pos.z;
        }
    }
}