using GLTFast;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AB.FSMAnimator
{
    public class FSMAnimator : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public async void LoadGltfBinaryFromMemory(string path)
        {
            //var filePath = "D:\\Downloads\\All_3bases_gearbox (2).glb";
            var filePath = path;
            byte[] data = File.ReadAllBytes(filePath);
            var gltf = new GltfImport();
            bool success = await gltf.LoadGltfBinary(
                data,
                // The URI of the original data is important for resolving relative URIs within the glTF
                new Uri(filePath)
                );
            if (success)
            {
                success = await gltf.InstantiateMainSceneAsync(transform);
            }
        }
    }
}

