using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Documents;
using GlmSharp;

namespace Manipulator
{
    public class Model
    {
        public Triangle[] mesh;

        public Model(string filename, vec4 color, bool useNormals = true)
        {
            using (StreamReader reader = File.OpenText(filename))
            {
                LoadFromReader(reader,color, useNormals);
            }
        }

        public Model(TextReader reader, vec4 color, bool useNormals = true)
        {
            LoadFromReader(reader,color, useNormals);
        }

        private void LoadFromReader(TextReader reader, vec4 color, bool calculateNormals)
        {
            List<Triangle> triangles = new List<Triangle>();
            List<vec3> vertices = new List<vec3>();
            List<vec2> uv = new List<vec2>();
            List<vec3> normals = new List<vec3>();
            bool missingNormalsInFaces = false;

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if(tokens.Length==0)
                    continue;
                if(tokens[0].StartsWith("#"))
                    continue;
                if(tokens[0]=="v")
                {
                    vertices.Add(new vec3(
                        float.Parse(tokens[1], CultureInfo.InvariantCulture),
                        float.Parse(tokens[2], CultureInfo.InvariantCulture),
                        float.Parse(tokens[3], CultureInfo.InvariantCulture)));
                }
                else if(tokens[0]=="vt")
                {
                    uv.Add(new vec2(
                        float.Parse(tokens[1], CultureInfo.InvariantCulture),
                        float.Parse(tokens[2], CultureInfo.InvariantCulture)));
                }
                else if(tokens[0]=="vn")
                {
                    normals.Add(new vec3(
                        float.Parse(tokens[1], CultureInfo.InvariantCulture),
                        float.Parse(tokens[2], CultureInfo.InvariantCulture),
                        float.Parse(tokens[3], CultureInfo.InvariantCulture)));
                }
                else if(tokens[0]=="f")
                {
                    for (int j = 2; j < tokens.Length; ++j)
                    {
                        Triangle t = new Triangle();

                        ParseFaceVertex(tokens[1], vertices.Count, normals.Count, out int aVertex, out int aNormal);
                        ParseFaceVertex(tokens[j - 1], vertices.Count, normals.Count, out int bVertex, out int bNormal);
                        ParseFaceVertex(tokens[j], vertices.Count, normals.Count, out int cVertex, out int cNormal);

                        t.a = vertices[aVertex];
                        t.b = vertices[bVertex];
                        t.c = vertices[cVertex];


                        if (aNormal >= 0 && bNormal >= 0 && cNormal >= 0)
                        {
                            t.normal_a = normals[aNormal];
                            t.normal_b = normals[bNormal];
                            t.normal_c = normals[cNormal];
                        }
                        else
                        {
                            missingNormalsInFaces = true;
                        }

                        t.color_a = color;
                        t.color_b = color;
                        t.color_c = color;

                        triangles.Add(t);
                    }
                }
            }

            mesh = triangles.ToArray();
            if(calculateNormals || missingNormalsInFaces || normals.Count == 0)
                Eng.CalculateNormals(mesh);
        }

        private static void ParseFaceVertex(string token, int vertexCount, int normalCount, out int vertexIndex, out int normalIndex)
        {
            string[] indices = token.Split('/');
            vertexIndex = ParseObjIndex(indices[0], vertexCount);

            normalIndex = -1;
            if (indices.Length >= 3 && !string.IsNullOrWhiteSpace(indices[2]) && normalCount > 0)
                normalIndex = ParseObjIndex(indices[2], normalCount);
        }

        private static int ParseObjIndex(string indexToken, int currentCount)
        {
            int parsed = int.Parse(indexToken, CultureInfo.InvariantCulture);
            return parsed > 0 ? parsed - 1 : currentCount + parsed;
        }
    }
}