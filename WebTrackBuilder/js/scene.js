import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

export function createScene(canvas) {
    const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    renderer.setClearColor(0x87ceeb);

    const scene = new THREE.Scene();
    scene.fog = new THREE.Fog(0x87ceeb, 60, 120);

    const camera = new THREE.PerspectiveCamera(50, 1, 0.1, 200);
    camera.position.set(20, 25, 20);

    const controls = new OrbitControls(camera, canvas);
    controls.enableDamping = true;
    controls.dampingFactor = 0.1;
    controls.maxPolarAngle = Math.PI / 2.05;
    controls.minDistance = 3;
    controls.maxDistance = 80;
    controls.target.set(15, 0, 15);

    // Ambient light
    const ambient = new THREE.AmbientLight(0xffffff, 0.6);
    scene.add(ambient);

    // Directional light with shadows
    const dirLight = new THREE.DirectionalLight(0xffffff, 0.8);
    dirLight.position.set(25, 35, 15);
    dirLight.castShadow = true;
    dirLight.shadow.mapSize.width = 2048;
    dirLight.shadow.mapSize.height = 2048;
    dirLight.shadow.camera.near = 1;
    dirLight.shadow.camera.far = 80;
    dirLight.shadow.camera.left = -40;
    dirLight.shadow.camera.right = 40;
    dirLight.shadow.camera.top = 40;
    dirLight.shadow.camera.bottom = -40;
    scene.add(dirLight);

    // Ground plane
    const groundGeo = new THREE.PlaneGeometry(120, 120);
    const groundMat = new THREE.MeshStandardMaterial({ color: 0x4a7c59, roughness: 0.9 });
    const ground = new THREE.Mesh(groundGeo, groundMat);
    ground.rotation.x = -Math.PI / 2;
    ground.position.set(15, -0.01, 15);
    ground.receiveShadow = true;
    ground.name = 'ground';
    scene.add(ground);

    return { renderer, scene, camera, controls };
}

export function createGrid(scene, gridSize) {
    const gridHelper = new THREE.GridHelper(gridSize, gridSize, 0x555555, 0x333333);
    gridHelper.position.set(gridSize / 2, 0.005, gridSize / 2);
    gridHelper.name = 'gridHelper';
    scene.add(gridHelper);
    return gridHelper;
}

export function resizeRenderer(renderer, camera, viewport) {
    const w = viewport.clientWidth;
    const h = viewport.clientHeight;
    if (renderer.domElement.width !== w || renderer.domElement.height !== h) {
        renderer.setSize(w, h, false);
        camera.aspect = w / h;
        camera.updateProjectionMatrix();
    }
}
