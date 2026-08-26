
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Random = System.Random;

namespace Utils
{
    public static class Utils
    {
        private const string PATH_TO_PARAMETERS = "Parameters/";
        
        private static readonly byte[] EncryptKey = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("aXBRV|Iu{R69EIqY}@6u%C{0hAvIWiR2"));
        private static readonly byte[] EncryptIV  = Encoding.UTF8.GetBytes("JzHJv%ACvXsd5*A~");

        public const float EPS = 1e-4f;
        public const float ASPECT_RATIO_MIN = 0.965f;
        public const float ASPECT_RATIO_MAX = 1f;
        public const float ASPECT_RATIO_4x3 = 4f / 3f;
        public const float ASPECT_RATIO_16x9 = 16f / 9f;
        public const float ASPECT_RATIO_18x9 = 18f / 9f;
        public const float ASPECT_RATIO_ULTRA_WIDE = 195f / 90f;

        public static Random Random = new();


        public static string TimeToString(int time)
        {
            string timeString = "";
            int hours = time / 3600;
            if (hours > 0)
            {
                timeString += hours + "ч : ";
            }
            int minutes = (time - hours * 3600) / 60;
            timeString += minutes + "м";
            if (hours == 0)
            {
                int seconds = time - minutes * 60;
                timeString += " : " + seconds + "c";
            }

            return timeString;
        }

        public static void Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                int k = Random.Next(0, n) % n;
                n--;
                (list[k], list[n]) = (list[n], list[k]);
            }
        }


        public static IEnumerator InvokeDelay(Action del, float delay)
        {
            yield return new WaitForSeconds(delay);
            del.Invoke();
        }

        public static IEnumerator InvokeDelay<T>(Action<T> del, T value, float delay)
        {
            yield return new WaitForSeconds(delay);
            del.Invoke(value);
        }


        public static float LerpByAspectRatio(float min, float max, float topAspect = ASPECT_RATIO_16x9,
            float lowAspect = ASPECT_RATIO_4x3)
        {
            Debug.Assert(Camera.main, "Camera.main != null");
            return Mathf.Lerp(min, max, (topAspect - Camera.main.aspect) / (topAspect - lowAspect));
        }


        public static void MoveToLayer(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root)
            {
                MoveToLayer(child, layer);
            }
        }


        public static T FromJson<T>(string key) where T : class
        {
            string text = Resources.Load<TextAsset>(PATH_TO_PARAMETERS + key).text;
            if (!string.IsNullOrEmpty(text))
            {
                try
                {
                    return JsonUtility.FromJson<T>(text);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error GetLoadJson: " + key + "  [" + e.Message + "]");
                }
            }

            Debug.LogError("FromJson return null! " + key);
            return null;
        }
        
        public static byte[] Encrypt(byte[] data)
        {
            using Aes aes = Aes.Create();
            aes.Key = EncryptKey;
            aes.IV = EncryptIV;

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        public static byte[] Decrypt(byte[] data)
        {
            using Aes aes = Aes.Create();
            aes.Key = EncryptKey;
            aes.IV = EncryptIV;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(data);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var result = new MemoryStream();
            cs.CopyTo(result);
            return result.ToArray();
        }
        
        
        #region MATH

        public static float Abs(float value)
        {
            return Less(value, 0.0f) ? -value : value;
        }

        public static bool Compare(Vector3 value1, Vector3 value2)
        {
            return Compare(value1.x, value2.x) && Compare(value1.y, value2.y) && Compare(value1.z, value2.z);
        }

        public static bool Compare(float value1, float value2)
        {
            float diff = value1 - value2;
            return Abs(diff) <= EPS;
        }

        public static int CompareTo(float value1, float value2)
        {
            if (Greater(value1, value2))
            {
                return 1;
            }
            else if (Less(value1, value2))
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        public static bool Less(float value1, float value2)
        {
            return value1 < value2 - EPS;
        }

        public static bool LessOrEqual(float value1, float value2)
        {
            return Less(value1, value2) || Compare(value1, value2);
        }

        public static bool Greater(float value1, float value2)
        {
            return value1 > value2 + EPS;
        }

        public static bool GreaterOrEqual(float value1, float value2)
        {
            return Greater(value1, value2) || Compare(value1, value2);
        }

        public static bool InRange(float value, float min, float max, bool strict = false)
        {
            if (strict)
            {
                return GreaterOrEqual(value, min) && LessOrEqual(value, max);
            }
            else
            {
                return Greater(value, min) && Less(value, max);
            }
        }

        #endregion

    }
}