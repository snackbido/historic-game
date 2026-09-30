using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PrehistoricTribe.EditorTools
{
    /// <summary>
    /// Helper dựng model placeholder low-poly từ primitive. Material và mesh được lưu thành
    /// asset thật vì prefab không nhúng được object tạo lúc chạy (cùng lý do như sprite trước đây).
    /// </summary>
    public static class EditorBuildUtils
    {
        private const string MaterialFolder = "Assets/Materials/Generated";
        private const string MeshFolder = "Assets/Models/Generated";

        public static Material Mat(string name, Color color, float emission = 0f)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                EnsureFolder(path);
                Shader shader = Shader.Find("Standard");
                mat = shader != null
                    ? new Material(shader)
                    : new Material(AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = color;
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.1f);
            if (emission > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emission);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Primitive có sẵn của Unity (Cube 1×1×1, Sphere Ø1, Cylinder Ø1 cao 2), bỏ collider.</summary>
        public static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 localPosition,
            Vector3 localScale, Material material, Vector3 localEuler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            Place(go.transform, parent, localPosition, localScale, localEuler);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>Hình nón (đáy Ø1 tại gốc, cao 1) — Unity không có primitive nón.</summary>
        public static GameObject Cone(Transform parent, string name, int sides, Vector3 localPosition,
            Vector3 localScale, Material material, Vector3 localEuler = default)
        {
            var go = new GameObject(name);
            Place(go.transform, parent, localPosition, localScale, localEuler);
            go.AddComponent<MeshFilter>().sharedMesh = ConeMesh(sides);
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static void Place(Transform t, Transform parent, Vector3 position, Vector3 scale, Vector3 euler)
        {
            if (parent != null) t.SetParent(parent, false);
            t.localPosition = position;
            t.localRotation = Quaternion.Euler(euler);
            t.localScale = scale;
        }

        /// <summary>Nón flat-shaded: đáy bán kính 0.5 ở y = 0, đỉnh ở y = 1.</summary>
        public static Mesh ConeMesh(int sides)
        {
            string path = $"{MeshFolder}/Cone{sides}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var tip = new Vector3(0f, 1f, 0f);

            for (int i = 0; i < sides; i++)
            {
                float a0 = Mathf.PI * 2f * i / sides;
                float a1 = Mathf.PI * 2f * (i + 1) / sides;
                var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f);
                var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);

                // Mỗi tam giác có đỉnh riêng → pháp tuyến phẳng (flat shading) kiểu low-poly.
                AddTriangle(vertices, triangles, tip, p1, p0);
                AddTriangle(vertices, triangles, Vector3.zero, p0, p1);
            }

            var mesh = new Mesh { name = $"Cone{sides}" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            EnsureFolder(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        public static GameObject SavePrefab(GameObject go, string path)
        {
            EnsureFolder(path);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>
        /// Hỏi trước khi ghi đè file đã có (tránh mất phần chỉnh tay / model thật đã thay vào).
        /// Batch mode không có người bấm nên luôn cho phép.
        /// </summary>
        public static bool ConfirmOverwrite(string title, string message, params string[] assetPaths)
        {
            if (Application.isBatchMode) return true;

            bool anyExists = false;
            foreach (string path in assetPaths)
                anyExists |= File.Exists(path);

            return !anyExists || EditorUtility.DisplayDialog(title, message, "Ghi đè", "Hủy");
        }

        public static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                Debug.LogError($"[EditorBuildUtils] Khong tim thay field '{fieldName}' tren {target.GetType()}");
                return;
            }
            field.SetValue(target, value);
        }

        public static void EnsureFolder(string assetPath)
        {
            string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;

            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
