using System;
using GlmSharp;

namespace Manipulator
{
    public class ManipulatorModel
    {
        private const float cylinderWidth=0.05f;
        private const float handLength=0.2f;

        //lengths of each segment
        public readonly float[] lengths= new float[7];

        //Rotation matrices for each joint
        //it simpler to store matrices instead of angles
        //6 joints for 7 segments
        private readonly mat3[] A = new mat3[6];

        //object position for manipulator to aim at
        public vec3 aim = new vec3(2,1,2);

        //current position of manipulator's hand
        public vec3 resPos{ private set; get; }


        public ManipulatorModel()
        {
            Reset();
        }

        //render manipulator
        public void Render(Eng eng, float dt)
        {
            //current rotation matrix
            mat3 rot=new mat3(1,0,0,
                              0,1,0,
                              0,0,1);
            //current translation vector
            vec3 translation=new vec3(0,0,0);


            for (int i = 0; i < 7; ++i)
            {
                //transform cylinder mesh to make a segment
                mat3 initialCylinderTransform = new mat3(cylinderWidth, 0, 0,
                    0, lengths[i], 0,
                    0, 0, cylinderWidth);



                //for segments with joints
                if (i < 6)
                {
                    eng.Render(Cylinder.triangles,new Transform(rot*initialCylinderTransform, translation));
                    translation += rot * new vec3(0, lengths[i], 0);
                    mat3 jointTransform = new mat3(cylinderWidth*2.0f, 0, 0,
                        0, cylinderWidth*2, 0,
                        0, 0, cylinderWidth*2.0f);
                    eng.Render(Cylinder.triangles,new Transform(rot*jointTransform, translation-rot*new vec3(0, cylinderWidth, 0)));

                    rot = rot * A[i];
                }
                else
                //for last segment with hand
                {
                    initialCylinderTransform.m11 -= handLength;
                    eng.Render(Cylinder.triangles,new Transform(rot*initialCylinderTransform, translation));

                    //resPos= translation + rot * new vec3(0, lengths[i], 0);
                    float closed =1-(resPos - aim).Length*10;
                    if (closed < 0)
                        closed = 0;
                    translation += rot * new vec3(0, lengths[i]-handLength, 0);

                    DrawHand(eng, new Transform(rot, translation),handLength,closed);
                }
            }
        }


        //draws manipulator hand with 3 fingers
        //closed parameter from 0 to 1 controls how much fingers are closed
        private void DrawHand(Eng eng, Transform transform, float size=0.2f, float closed=0.2f)
        {
            //don't close completely to avoid fingers overlap
            closed *= 0.8f;

            const float width = 0.02f;
            float length = size;
            const float pi3 = (float)Math.PI / 3;
            const float angle = (float)Math.PI / 5;

            Transform initial = new Transform(new mat3(width, 0, 0,
                0, length, 0,
                0, 0, width), new vec3(0, 0, 0));

            Transform r1 = initial;
            r1.Rotate(new vec3(1,0,0), angle);

            Transform r2 = initial;
            r2.Rotate(new vec3(glm.Cos(pi3*2),0,glm.Sin(pi3*2)), angle);

            Transform r3 = initial;
            r3.Rotate(new vec3(glm.Cos(-pi3*2),0,glm.Sin(-pi3*2)), angle);

            eng.Render(Cylinder.triangles, transform*r1);
            eng.Render(Cylinder.triangles, transform*r2);
            eng.Render(Cylinder.triangles, transform*r3);

            vec3 up = new vec3(0, 1, 0);

            Transform m1 = new Transform(r1.rotation * up);
            Transform m2 = new Transform(r2.rotation * up);
            Transform m3 = new Transform(r3.rotation * up);

            r1 = initial;
            r1.Rotate(new vec3(1,0,0), -angle*closed);

            r2 = initial;
            r2.Rotate(new vec3(glm.Cos(pi3*2),0,glm.Sin(pi3*2)), -angle*closed);

            r3 = initial;
            r3.Rotate(new vec3(glm.Cos(-pi3*2),0,glm.Sin(-pi3*2)), -angle*closed);

            m1=m1*r1;
            m2=m2*r2;
            m3=m3*r3;



            eng.Render(Sphere.triangles, mat3.Identity*width*1.1f, (transform*m1).translation);
            eng.Render(Sphere.triangles, mat3.Identity*width*1.1f, (transform*m2).translation);
            eng.Render(Sphere.triangles, mat3.Identity*width*1.1f, (transform*m3).translation);

            eng.Render(Cylinder.triangles, transform*m1);
            eng.Render(Cylinder.triangles, transform*m2);
            eng.Render(Cylinder.triangles, transform*m3);


            var t_up = new Transform(new vec3(0, 1, 0));
            eng.Render(Sphere.triangles, mat3.Identity*width*1.1f, (transform*m1*t_up).translation);
            eng.Render(Sphere.triangles, mat3.Identity*width*1.1f, (transform*m2*t_up).translation);
            eng.Render(Sphere.triangles, mat3.Identity*width*1.1f, (transform*m3*t_up).translation);
        }



        //resets manipulator angles (rotation matrices)
        public void Reset()
        {
            for (int i = 0; i < 6; ++i)
            {
                A[i] = new mat3(-1, 0, 0,
                                 0, 0, 1,
                                 0, 1, 0).Transposed;

            }
        }


        //Optimization step
        //maxAngle - maximal change allowed for each angle
        public void InverseKinematics(float maxAngle)
        {
            vec3 OA = aim;

            mat3 R = new mat3(1, 0, 0,
                0, 1, 0,
                0, 0, 1);
            vec3 OO7 = new vec3(0, 0, 0);
            for (int i = 0; i < 7; ++i)
            {
                OO7 = OO7 + R * new vec3(0, lengths[i], 0);
                if(i<6)
                    R = R * A[i];
            }

            resPos= OO7;

            for (int j = 0; j < 6; ++j)
            {
                vec3 O1 = new vec3(0, lengths[j], 0);

                vec3 O1A = A[j].Transposed * (OA - O1);
                vec3 O1O7 = A[j].Transposed * (OO7 - O1);

                //evaluate angle between projections of aim and current position of the hand on rotation plane of current joint
                //within coordinate system associated with current joint (where this joint is the origin)
                float dot = glm.Dot(O1A.xy, O1O7.xy);
                float cross = glm.Cross(O1O7.xy, O1A.xy);

                float da = (float)Math.Acos(dot /O1A.xy.Length / O1O7.xy.Length);

                //in case if hand on this joint (0107==0) or the aim is on this joint(O1A==0) do not rotate this joint on this step
                if (float.IsNaN(da) || float.IsNegativeInfinity(da) || float.IsPositiveInfinity(da))
                    da = 0;

                if (cross < 0)
                    da = - da;

                //to move with small steps limit angle change to maxAngle
                if(da>maxAngle)
                    da=maxAngle;
                if(da<-maxAngle)
                    da=-maxAngle;

                //apply da rotation to the rotation matrix of corresponding joint
                mat3 m = new mat3((float)Math.Cos(da), -(float)Math.Sin(da), 0,
                    (float)Math.Sin(da), (float)Math.Cos(da), 0,
                    0, 0, 1).Transposed;
                A[j]=A[j]*m;


                OA = m.Transposed * O1A;
                OO7 = O1O7;
            }
        }
    }
}