import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';

const MODEL_BASE_PATH = './Assets/Racing%20Kit/Models/GLTF%20format/';
const loader = new GLTFLoader();
const modelCache = new Map();

// Offscreen thumbnail renderer
let thumbRenderer = null;
let thumbScene = null;
let thumbCamera = null;
const THUMB_SIZE = 64;

function initThumbRenderer() {
    thumbRenderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    thumbRenderer.setSize(THUMB_SIZE, THUMB_SIZE);
    thumbRenderer.setClearColor(0x000000, 0);

    thumbScene = new THREE.Scene();
    const ambient = new THREE.AmbientLight(0xffffff, 0.7);
    thumbScene.add(ambient);
    const dir = new THREE.DirectionalLight(0xffffff, 0.8);
    dir.position.set(2, 3, 1);
    thumbScene.add(dir);

    thumbCamera = new THREE.PerspectiveCamera(40, 1, 0.01, 50);
}

export function renderThumbnail(modelId) {
    if (!thumbRenderer) initThumbRenderer();
    const original = modelCache.get(modelId);
    if (!original) return null;

    const clone = original.clone();
    thumbScene.add(clone);

    // Fit model in view
    const box = new THREE.Box3().setFromObject(clone);
    const center = box.getCenter(new THREE.Vector3());
    const size = box.getSize(new THREE.Vector3());
    const maxDim = Math.max(size.x, size.y, size.z) || 1;
    const dist = maxDim * 1.8;
    thumbCamera.position.set(center.x + dist * 0.6, center.y + dist * 0.7, center.z + dist * 0.6);
    thumbCamera.lookAt(center);

    thumbRenderer.render(thumbScene, thumbCamera);
    const dataUrl = thumbRenderer.domElement.toDataURL('image/png');
    thumbScene.remove(clone);
    return dataUrl;
}

export async function preloadAll(catalog, onProgress) {
    let loaded = 0;
    const total = catalog.length;

    const promises = catalog.map(piece => {
        if (piece.custom) {
            loaded++;
            if (onProgress) onProgress(loaded, total);
            return Promise.resolve();
        }
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

export function registerCustomModel(modelId, threeGroup) {
    modelCache.set(modelId, threeGroup);
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
