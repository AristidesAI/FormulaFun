import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';

const MODEL_BASE_PATH = '../Assets/Racing%20Kit/Models/GLTF%20format/';
const loader = new GLTFLoader();
const modelCache = new Map();

export async function preloadAll(catalog, onProgress) {
    let loaded = 0;
    const total = catalog.length;

    const promises = catalog.map(piece => {
        const url = `${MODEL_BASE_PATH}${piece.modelId}.glb`;
        return loader.loadAsync(url).then(gltf => {
            const model = gltf.scene;
            model.traverse(child => {
                if (child.isMesh) {
                    child.castShadow = true;
                    child.receiveShadow = true;
                }
            });
            modelCache.set(piece.modelId, model);
            loaded++;
            if (onProgress) onProgress(loaded, total);
        }).catch(err => {
            console.warn(`Failed to load ${piece.modelId}:`, err);
            loaded++;
            if (onProgress) onProgress(loaded, total);
        });
    });

    await Promise.all(promises);
}

export function getClone(modelId) {
    const original = modelCache.get(modelId);
    if (!original) {
        console.warn(`Model not in cache: ${modelId}`);
        return null;
    }
    return original.clone();
}

export function isLoaded(modelId) {
    return modelCache.has(modelId);
}
