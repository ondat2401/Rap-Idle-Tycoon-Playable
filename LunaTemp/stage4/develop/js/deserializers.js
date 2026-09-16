var Deserializers = {}
Deserializers["UnityEngine.JointSpring"] = function (request, data, root) {
  var i402 = root || request.c( 'UnityEngine.JointSpring' )
  var i403 = data
  i402.spring = i403[0]
  i402.damper = i403[1]
  i402.targetPosition = i403[2]
  return i402
}

Deserializers["UnityEngine.JointMotor"] = function (request, data, root) {
  var i404 = root || request.c( 'UnityEngine.JointMotor' )
  var i405 = data
  i404.m_TargetVelocity = i405[0]
  i404.m_Force = i405[1]
  i404.m_FreeSpin = i405[2]
  return i404
}

Deserializers["UnityEngine.JointLimits"] = function (request, data, root) {
  var i406 = root || request.c( 'UnityEngine.JointLimits' )
  var i407 = data
  i406.m_Min = i407[0]
  i406.m_Max = i407[1]
  i406.m_Bounciness = i407[2]
  i406.m_BounceMinVelocity = i407[3]
  i406.m_ContactDistance = i407[4]
  i406.minBounce = i407[5]
  i406.maxBounce = i407[6]
  return i406
}

Deserializers["UnityEngine.JointDrive"] = function (request, data, root) {
  var i408 = root || request.c( 'UnityEngine.JointDrive' )
  var i409 = data
  i408.m_PositionSpring = i409[0]
  i408.m_PositionDamper = i409[1]
  i408.m_MaximumForce = i409[2]
  i408.m_UseAcceleration = i409[3]
  return i408
}

Deserializers["UnityEngine.SoftJointLimitSpring"] = function (request, data, root) {
  var i410 = root || request.c( 'UnityEngine.SoftJointLimitSpring' )
  var i411 = data
  i410.m_Spring = i411[0]
  i410.m_Damper = i411[1]
  return i410
}

Deserializers["UnityEngine.SoftJointLimit"] = function (request, data, root) {
  var i412 = root || request.c( 'UnityEngine.SoftJointLimit' )
  var i413 = data
  i412.m_Limit = i413[0]
  i412.m_Bounciness = i413[1]
  i412.m_ContactDistance = i413[2]
  return i412
}

Deserializers["UnityEngine.WheelFrictionCurve"] = function (request, data, root) {
  var i414 = root || request.c( 'UnityEngine.WheelFrictionCurve' )
  var i415 = data
  i414.m_ExtremumSlip = i415[0]
  i414.m_ExtremumValue = i415[1]
  i414.m_AsymptoteSlip = i415[2]
  i414.m_AsymptoteValue = i415[3]
  i414.m_Stiffness = i415[4]
  return i414
}

Deserializers["UnityEngine.JointAngleLimits2D"] = function (request, data, root) {
  var i416 = root || request.c( 'UnityEngine.JointAngleLimits2D' )
  var i417 = data
  i416.m_LowerAngle = i417[0]
  i416.m_UpperAngle = i417[1]
  return i416
}

Deserializers["UnityEngine.JointMotor2D"] = function (request, data, root) {
  var i418 = root || request.c( 'UnityEngine.JointMotor2D' )
  var i419 = data
  i418.m_MotorSpeed = i419[0]
  i418.m_MaximumMotorTorque = i419[1]
  return i418
}

Deserializers["UnityEngine.JointSuspension2D"] = function (request, data, root) {
  var i420 = root || request.c( 'UnityEngine.JointSuspension2D' )
  var i421 = data
  i420.m_DampingRatio = i421[0]
  i420.m_Frequency = i421[1]
  i420.m_Angle = i421[2]
  return i420
}

Deserializers["UnityEngine.JointTranslationLimits2D"] = function (request, data, root) {
  var i422 = root || request.c( 'UnityEngine.JointTranslationLimits2D' )
  var i423 = data
  i422.m_LowerTranslation = i423[0]
  i422.m_UpperTranslation = i423[1]
  return i422
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Material"] = function (request, data, root) {
  var i424 = root || new pc.UnityMaterial()
  var i425 = data
  i424.name = i425[0]
  request.r(i425[1], i425[2], 0, i424, 'shader')
  i424.renderQueue = i425[3]
  i424.enableInstancing = !!i425[4]
  var i427 = i425[5]
  var i426 = []
  for(var i = 0; i < i427.length; i += 1) {
    i426.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Material+FloatParameter', i427[i + 0]) );
  }
  i424.floatParameters = i426
  var i429 = i425[6]
  var i428 = []
  for(var i = 0; i < i429.length; i += 1) {
    i428.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Material+ColorParameter', i429[i + 0]) );
  }
  i424.colorParameters = i428
  var i431 = i425[7]
  var i430 = []
  for(var i = 0; i < i431.length; i += 1) {
    i430.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Material+VectorParameter', i431[i + 0]) );
  }
  i424.vectorParameters = i430
  var i433 = i425[8]
  var i432 = []
  for(var i = 0; i < i433.length; i += 1) {
    i432.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Material+TextureParameter', i433[i + 0]) );
  }
  i424.textureParameters = i432
  var i435 = i425[9]
  var i434 = []
  for(var i = 0; i < i435.length; i += 1) {
    i434.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Material+MaterialFlag', i435[i + 0]) );
  }
  i424.materialFlags = i434
  return i424
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Material+FloatParameter"] = function (request, data, root) {
  var i438 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Material+FloatParameter' )
  var i439 = data
  i438.name = i439[0]
  i438.value = i439[1]
  return i438
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Material+ColorParameter"] = function (request, data, root) {
  var i442 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Material+ColorParameter' )
  var i443 = data
  i442.name = i443[0]
  i442.value = new pc.Color(i443[1], i443[2], i443[3], i443[4])
  return i442
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Material+VectorParameter"] = function (request, data, root) {
  var i446 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Material+VectorParameter' )
  var i447 = data
  i446.name = i447[0]
  i446.value = new pc.Vec4( i447[1], i447[2], i447[3], i447[4] )
  return i446
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Material+TextureParameter"] = function (request, data, root) {
  var i450 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Material+TextureParameter' )
  var i451 = data
  i450.name = i451[0]
  request.r(i451[1], i451[2], 0, i450, 'value')
  return i450
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Material+MaterialFlag"] = function (request, data, root) {
  var i454 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Material+MaterialFlag' )
  var i455 = data
  i454.name = i455[0]
  i454.enabled = !!i455[1]
  return i454
}

Deserializers["Luna.Unity.DTO.UnityEngine.Textures.Texture2D"] = function (request, data, root) {
  var i456 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Textures.Texture2D' )
  var i457 = data
  i456.name = i457[0]
  i456.width = i457[1]
  i456.height = i457[2]
  i456.mipmapCount = i457[3]
  i456.anisoLevel = i457[4]
  i456.filterMode = i457[5]
  i456.hdr = !!i457[6]
  i456.format = i457[7]
  i456.wrapMode = i457[8]
  i456.alphaIsTransparency = !!i457[9]
  i456.alphaSource = i457[10]
  i456.graphicsFormat = i457[11]
  i456.sRGBTexture = !!i457[12]
  i456.desiredColorSpace = i457[13]
  i456.wrapU = i457[14]
  i456.wrapV = i457[15]
  return i456
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Mesh"] = function (request, data, root) {
  var i458 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Mesh' )
  var i459 = data
  i458.name = i459[0]
  i458.halfPrecision = !!i459[1]
  i458.useSimplification = !!i459[2]
  i458.useUInt32IndexFormat = !!i459[3]
  i458.vertexCount = i459[4]
  i458.aabb = i459[5]
  var i461 = i459[6]
  var i460 = []
  for(var i = 0; i < i461.length; i += 1) {
    i460.push( !!i461[i + 0] );
  }
  i458.streams = i460
  i458.vertices = i459[7]
  var i463 = i459[8]
  var i462 = []
  for(var i = 0; i < i463.length; i += 1) {
    i462.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Mesh+SubMesh', i463[i + 0]) );
  }
  i458.subMeshes = i462
  var i465 = i459[9]
  var i464 = []
  for(var i = 0; i < i465.length; i += 16) {
    i464.push( new pc.Mat4().setData(i465[i + 0], i465[i + 1], i465[i + 2], i465[i + 3],  i465[i + 4], i465[i + 5], i465[i + 6], i465[i + 7],  i465[i + 8], i465[i + 9], i465[i + 10], i465[i + 11],  i465[i + 12], i465[i + 13], i465[i + 14], i465[i + 15]) );
  }
  i458.bindposes = i464
  var i467 = i459[10]
  var i466 = []
  for(var i = 0; i < i467.length; i += 1) {
    i466.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShape', i467[i + 0]) );
  }
  i458.blendShapes = i466
  return i458
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Mesh+SubMesh"] = function (request, data, root) {
  var i472 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Mesh+SubMesh' )
  var i473 = data
  i472.triangles = i473[0]
  return i472
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShape"] = function (request, data, root) {
  var i478 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShape' )
  var i479 = data
  i478.name = i479[0]
  var i481 = i479[1]
  var i480 = []
  for(var i = 0; i < i481.length; i += 1) {
    i480.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShapeFrame', i481[i + 0]) );
  }
  i478.frames = i480
  return i478
}

Deserializers["Luna.Unity.DTO.UnityEngine.Scene.Scene"] = function (request, data, root) {
  var i482 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Scene.Scene' )
  var i483 = data
  i482.name = i483[0]
  i482.index = i483[1]
  i482.startup = !!i483[2]
  return i482
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.Camera"] = function (request, data, root) {
  var i484 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.Camera' )
  var i485 = data
  i484.aspect = i485[0]
  i484.orthographic = !!i485[1]
  i484.orthographicSize = i485[2]
  i484.backgroundColor = new pc.Color(i485[3], i485[4], i485[5], i485[6])
  i484.nearClipPlane = i485[7]
  i484.farClipPlane = i485[8]
  i484.fieldOfView = i485[9]
  i484.depth = i485[10]
  i484.clearFlags = i485[11]
  i484.cullingMask = i485[12]
  i484.rect = i485[13]
  request.r(i485[14], i485[15], 0, i484, 'targetTexture')
  i484.usePhysicalProperties = !!i485[16]
  i484.focalLength = i485[17]
  i484.sensorSize = new pc.Vec2( i485[18], i485[19] )
  i484.lensShift = new pc.Vec2( i485[20], i485[21] )
  i484.gateFit = i485[22]
  i484.commandBufferCount = i485[23]
  i484.cameraType = i485[24]
  i484.enabled = !!i485[25]
  return i484
}

Deserializers["Luna.Unity.DTO.UnityEngine.Scene.GameObject"] = function (request, data, root) {
  var i486 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Scene.GameObject' )
  var i487 = data
  i486.name = i487[0]
  i486.tagId = i487[1]
  i486.enabled = !!i487[2]
  i486.isStatic = !!i487[3]
  i486.layer = i487[4]
  return i486
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.MeshRenderer"] = function (request, data, root) {
  var i488 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.MeshRenderer' )
  var i489 = data
  request.r(i489[0], i489[1], 0, i488, 'additionalVertexStreams')
  i488.enabled = !!i489[2]
  request.r(i489[3], i489[4], 0, i488, 'sharedMaterial')
  var i491 = i489[5]
  var i490 = []
  for(var i = 0; i < i491.length; i += 2) {
  request.r(i491[i + 0], i491[i + 1], 2, i490, '')
  }
  i488.sharedMaterials = i490
  i488.receiveShadows = !!i489[6]
  i488.shadowCastingMode = i489[7]
  i488.sortingLayerID = i489[8]
  i488.sortingOrder = i489[9]
  i488.lightmapIndex = i489[10]
  i488.lightmapSceneIndex = i489[11]
  i488.lightmapScaleOffset = new pc.Vec4( i489[12], i489[13], i489[14], i489[15] )
  i488.lightProbeUsage = i489[16]
  i488.reflectionProbeUsage = i489[17]
  return i488
}

Deserializers["Spine.Unity.SkeletonAnimation"] = function (request, data, root) {
  var i494 = root || request.c( 'Spine.Unity.SkeletonAnimation' )
  var i495 = data
  i494.loop = !!i495[0]
  i494.timeScale = i495[1]
  request.r(i495[2], i495[3], 0, i494, 'skeletonDataAsset')
  i494.initialSkinName = i495[4]
  i494.fixPrefabOverrideViaMeshFilter = i495[5]
  i494.initialFlipX = !!i495[6]
  i494.initialFlipY = !!i495[7]
  i494.updateWhenInvisible = i495[8]
  i494.zSpacing = i495[9]
  i494.useClipping = !!i495[10]
  i494.immutableTriangles = !!i495[11]
  i494.pmaVertexColors = !!i495[12]
  i494.clearStateOnDisable = !!i495[13]
  i494.tintBlack = !!i495[14]
  i494.singleSubmesh = !!i495[15]
  i494.fixDrawOrder = !!i495[16]
  i494.addNormals = !!i495[17]
  i494.calculateTangents = !!i495[18]
  i494.maskInteraction = i495[19]
  i494.maskMaterials = request.d('Spine.Unity.SkeletonRenderer+SpriteMaskInteractionMaterials', i495[20], i494.maskMaterials)
  i494.disableRenderingOnOverride = !!i495[21]
  i494.updateTiming = i495[22]
  i494.unscaledTime = !!i495[23]
  i494._animationName = i495[24]
  var i497 = i495[25]
  var i496 = []
  for(var i = 0; i < i497.length; i += 1) {
    i496.push( i497[i + 0] );
  }
  i494.separatorSlotNames = i496
  i494.physicsPositionInheritanceFactor = new pc.Vec2( i495[26], i495[27] )
  i494.physicsRotationInheritanceFactor = i495[28]
  request.r(i495[29], i495[30], 0, i494, 'physicsMovementRelativeTo')
  return i494
}

Deserializers["Spine.Unity.SkeletonRenderer+SpriteMaskInteractionMaterials"] = function (request, data, root) {
  var i498 = root || request.c( 'Spine.Unity.SkeletonRenderer+SpriteMaskInteractionMaterials' )
  var i499 = data
  var i501 = i499[0]
  var i500 = []
  for(var i = 0; i < i501.length; i += 2) {
  request.r(i501[i + 0], i501[i + 1], 2, i500, '')
  }
  i498.materialsMaskDisabled = i500
  var i503 = i499[1]
  var i502 = []
  for(var i = 0; i < i503.length; i += 2) {
  request.r(i503[i + 0], i503[i + 1], 2, i502, '')
  }
  i498.materialsInsideMask = i502
  var i505 = i499[2]
  var i504 = []
  for(var i = 0; i < i505.length; i += 2) {
  request.r(i505[i + 0], i505[i + 1], 2, i504, '')
  }
  i498.materialsOutsideMask = i504
  return i498
}

Deserializers["Luna.Unity.DTO.UnityEngine.Components.MeshFilter"] = function (request, data, root) {
  var i508 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Components.MeshFilter' )
  var i509 = data
  request.r(i509[0], i509[1], 0, i508, 'sharedMesh')
  return i508
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.RenderSettings"] = function (request, data, root) {
  var i510 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.RenderSettings' )
  var i511 = data
  i510.ambientIntensity = i511[0]
  i510.reflectionIntensity = i511[1]
  i510.ambientMode = i511[2]
  i510.ambientLight = new pc.Color(i511[3], i511[4], i511[5], i511[6])
  i510.ambientSkyColor = new pc.Color(i511[7], i511[8], i511[9], i511[10])
  i510.ambientGroundColor = new pc.Color(i511[11], i511[12], i511[13], i511[14])
  i510.ambientEquatorColor = new pc.Color(i511[15], i511[16], i511[17], i511[18])
  i510.fogColor = new pc.Color(i511[19], i511[20], i511[21], i511[22])
  i510.fogEndDistance = i511[23]
  i510.fogStartDistance = i511[24]
  i510.fogDensity = i511[25]
  i510.fog = !!i511[26]
  request.r(i511[27], i511[28], 0, i510, 'skybox')
  i510.fogMode = i511[29]
  var i513 = i511[30]
  var i512 = []
  for(var i = 0; i < i513.length; i += 1) {
    i512.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.RenderSettings+Lightmap', i513[i + 0]) );
  }
  i510.lightmaps = i512
  i510.lightProbes = request.d('Luna.Unity.DTO.UnityEngine.Assets.RenderSettings+LightProbes', i511[31], i510.lightProbes)
  i510.lightmapsMode = i511[32]
  i510.mixedBakeMode = i511[33]
  i510.environmentLightingMode = i511[34]
  i510.ambientProbe = new pc.SphericalHarmonicsL2(i511[35])
  request.r(i511[36], i511[37], 0, i510, 'customReflection')
  request.r(i511[38], i511[39], 0, i510, 'defaultReflection')
  i510.defaultReflectionMode = i511[40]
  i510.defaultReflectionResolution = i511[41]
  i510.sunLightObjectId = i511[42]
  i510.pixelLightCount = i511[43]
  i510.defaultReflectionHDR = !!i511[44]
  i510.hasLightDataAsset = !!i511[45]
  i510.hasManualGenerate = !!i511[46]
  return i510
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.RenderSettings+Lightmap"] = function (request, data, root) {
  var i516 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.RenderSettings+Lightmap' )
  var i517 = data
  request.r(i517[0], i517[1], 0, i516, 'lightmapColor')
  request.r(i517[2], i517[3], 0, i516, 'lightmapDirection')
  request.r(i517[4], i517[5], 0, i516, 'shadowMask')
  return i516
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.RenderSettings+LightProbes"] = function (request, data, root) {
  var i518 = root || new UnityEngine.LightProbes()
  var i519 = data
  return i518
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader"] = function (request, data, root) {
  var i526 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader' )
  var i527 = data
  var i529 = i527[0]
  var i528 = new (System.Collections.Generic.List$1(Bridge.ns('Luna.Unity.DTO.UnityEngine.Assets.Shader+ShaderCompilationError')))
  for(var i = 0; i < i529.length; i += 1) {
    i528.add(request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+ShaderCompilationError', i529[i + 0]));
  }
  i526.ShaderCompilationErrors = i528
  i526.name = i527[1]
  i526.guid = i527[2]
  var i531 = i527[3]
  var i530 = []
  for(var i = 0; i < i531.length; i += 1) {
    i530.push( i531[i + 0] );
  }
  i526.shaderDefinedKeywords = i530
  var i533 = i527[4]
  var i532 = []
  for(var i = 0; i < i533.length; i += 1) {
    i532.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass', i533[i + 0]) );
  }
  i526.passes = i532
  var i535 = i527[5]
  var i534 = []
  for(var i = 0; i < i535.length; i += 1) {
    i534.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+UsePass', i535[i + 0]) );
  }
  i526.usePasses = i534
  var i537 = i527[6]
  var i536 = []
  for(var i = 0; i < i537.length; i += 1) {
    i536.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+DefaultParameterValue', i537[i + 0]) );
  }
  i526.defaultParameterValues = i536
  request.r(i527[7], i527[8], 0, i526, 'unityFallbackShader')
  i526.readDepth = !!i527[9]
  i526.hasDepthOnlyPass = !!i527[10]
  i526.isCreatedByShaderGraph = !!i527[11]
  i526.disableBatching = !!i527[12]
  i526.compiled = !!i527[13]
  return i526
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+ShaderCompilationError"] = function (request, data, root) {
  var i540 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+ShaderCompilationError' )
  var i541 = data
  i540.shaderName = i541[0]
  i540.errorMessage = i541[1]
  return i540
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass"] = function (request, data, root) {
  var i544 = root || new pc.UnityShaderPass()
  var i545 = data
  i544.id = i545[0]
  i544.subShaderIndex = i545[1]
  i544.name = i545[2]
  i544.passType = i545[3]
  i544.grabPassTextureName = i545[4]
  i544.usePass = !!i545[5]
  i544.zTest = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i545[6], i544.zTest)
  i544.zWrite = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i545[7], i544.zWrite)
  i544.culling = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i545[8], i544.culling)
  i544.blending = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Blending', i545[9], i544.blending)
  i544.alphaBlending = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Blending', i545[10], i544.alphaBlending)
  i544.colorWriteMask = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i545[11], i544.colorWriteMask)
  i544.offsetUnits = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i545[12], i544.offsetUnits)
  i544.offsetFactor = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i545[13], i544.offsetFactor)
  i544.stencilRef = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i545[14], i544.stencilRef)
  i544.stencilReadMask = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i545[15], i544.stencilReadMask)
  i544.stencilWriteMask = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i545[16], i544.stencilWriteMask)
  i544.stencilOp = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+StencilOp', i545[17], i544.stencilOp)
  i544.stencilOpFront = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+StencilOp', i545[18], i544.stencilOpFront)
  i544.stencilOpBack = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+StencilOp', i545[19], i544.stencilOpBack)
  var i547 = i545[20]
  var i546 = []
  for(var i = 0; i < i547.length; i += 1) {
    i546.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Tag', i547[i + 0]) );
  }
  i544.tags = i546
  var i549 = i545[21]
  var i548 = []
  for(var i = 0; i < i549.length; i += 1) {
    i548.push( i549[i + 0] );
  }
  i544.passDefinedKeywords = i548
  var i551 = i545[22]
  var i550 = []
  for(var i = 0; i < i551.length; i += 1) {
    i550.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+KeywordGroup', i551[i + 0]) );
  }
  i544.passDefinedKeywordGroups = i550
  var i553 = i545[23]
  var i552 = []
  for(var i = 0; i < i553.length; i += 1) {
    i552.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Variant', i553[i + 0]) );
  }
  i544.variants = i552
  var i555 = i545[24]
  var i554 = []
  for(var i = 0; i < i555.length; i += 1) {
    i554.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Variant', i555[i + 0]) );
  }
  i544.excludedVariants = i554
  i544.hasDepthReader = !!i545[25]
  return i544
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value"] = function (request, data, root) {
  var i556 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value' )
  var i557 = data
  i556.val = i557[0]
  i556.name = i557[1]
  return i556
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Blending"] = function (request, data, root) {
  var i558 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Blending' )
  var i559 = data
  i558.src = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i559[0], i558.src)
  i558.dst = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i559[1], i558.dst)
  i558.op = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i559[2], i558.op)
  return i558
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+StencilOp"] = function (request, data, root) {
  var i560 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+StencilOp' )
  var i561 = data
  i560.pass = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i561[0], i560.pass)
  i560.fail = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i561[1], i560.fail)
  i560.zFail = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i561[2], i560.zFail)
  i560.comp = request.d('Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value', i561[3], i560.comp)
  return i560
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Tag"] = function (request, data, root) {
  var i564 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Tag' )
  var i565 = data
  i564.name = i565[0]
  i564.value = i565[1]
  return i564
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+KeywordGroup"] = function (request, data, root) {
  var i568 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+KeywordGroup' )
  var i569 = data
  var i571 = i569[0]
  var i570 = []
  for(var i = 0; i < i571.length; i += 1) {
    i570.push( i571[i + 0] );
  }
  i568.keywords = i570
  i568.hasDiscard = !!i569[1]
  return i568
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Variant"] = function (request, data, root) {
  var i574 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Variant' )
  var i575 = data
  i574.passId = i575[0]
  i574.subShaderIndex = i575[1]
  var i577 = i575[2]
  var i576 = []
  for(var i = 0; i < i577.length; i += 1) {
    i576.push( i577[i + 0] );
  }
  i574.keywords = i576
  i574.vertexProgram = i575[3]
  i574.fragmentProgram = i575[4]
  i574.exportedForWebGl2 = !!i575[5]
  i574.readDepth = !!i575[6]
  return i574
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+UsePass"] = function (request, data, root) {
  var i580 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+UsePass' )
  var i581 = data
  request.r(i581[0], i581[1], 0, i580, 'shader')
  i580.pass = i581[2]
  return i580
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Shader+DefaultParameterValue"] = function (request, data, root) {
  var i584 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Shader+DefaultParameterValue' )
  var i585 = data
  i584.name = i585[0]
  i584.type = i585[1]
  i584.value = new pc.Vec4( i585[2], i585[3], i585[4], i585[5] )
  i584.textureValue = i585[6]
  i584.shaderPropertyFlag = i585[7]
  return i584
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Font"] = function (request, data, root) {
  var i586 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Font' )
  var i587 = data
  i586.name = i587[0]
  i586.ascent = i587[1]
  i586.originalLineHeight = i587[2]
  i586.fontSize = i587[3]
  var i589 = i587[4]
  var i588 = []
  for(var i = 0; i < i589.length; i += 1) {
    i588.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Font+CharacterInfo', i589[i + 0]) );
  }
  i586.characterInfo = i588
  request.r(i587[5], i587[6], 0, i586, 'texture')
  i586.originalFontSize = i587[7]
  return i586
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Font+CharacterInfo"] = function (request, data, root) {
  var i592 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Font+CharacterInfo' )
  var i593 = data
  i592.index = i593[0]
  i592.advance = i593[1]
  i592.bearing = i593[2]
  i592.glyphWidth = i593[3]
  i592.glyphHeight = i593[4]
  i592.minX = i593[5]
  i592.maxX = i593[6]
  i592.minY = i593[7]
  i592.maxY = i593[8]
  i592.uvBottomLeftX = i593[9]
  i592.uvBottomLeftY = i593[10]
  i592.uvBottomRightX = i593[11]
  i592.uvBottomRightY = i593[12]
  i592.uvTopLeftX = i593[13]
  i592.uvTopLeftY = i593[14]
  i592.uvTopRightX = i593[15]
  i592.uvTopRightY = i593[16]
  return i592
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.TextAsset"] = function (request, data, root) {
  var i594 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.TextAsset' )
  var i595 = data
  i594.name = i595[0]
  i594.bytes64 = i595[1]
  i594.data = i595[2]
  return i594
}

Deserializers["Spine.Unity.SkeletonDataAsset"] = function (request, data, root) {
  var i596 = root || request.c( 'Spine.Unity.SkeletonDataAsset' )
  var i597 = data
  var i599 = i597[0]
  var i598 = []
  for(var i = 0; i < i599.length; i += 2) {
  request.r(i599[i + 0], i599[i + 1], 2, i598, '')
  }
  i596.atlasAssets = i598
  i596.scale = i597[1]
  request.r(i597[2], i597[3], 0, i596, 'skeletonJSON')
  i596.isUpgradingBlendModeMaterials = !!i597[4]
  i596.blendModeMaterials = request.d('Spine.Unity.BlendModeMaterials', i597[5], i596.blendModeMaterials)
  var i601 = i597[6]
  var i600 = new (System.Collections.Generic.List$1(Bridge.ns('Spine.Unity.SkeletonDataModifierAsset')))
  for(var i = 0; i < i601.length; i += 2) {
  request.r(i601[i + 0], i601[i + 1], 1, i600, '')
  }
  i596.skeletonDataModifiers = i600
  var i603 = i597[7]
  var i602 = []
  for(var i = 0; i < i603.length; i += 1) {
    i602.push( i603[i + 0] );
  }
  i596.fromAnimation = i602
  var i605 = i597[8]
  var i604 = []
  for(var i = 0; i < i605.length; i += 1) {
    i604.push( i605[i + 0] );
  }
  i596.toAnimation = i604
  i596.duration = i597[9]
  i596.defaultMix = i597[10]
  request.r(i597[11], i597[12], 0, i596, 'controller')
  return i596
}

Deserializers["Spine.Unity.BlendModeMaterials"] = function (request, data, root) {
  var i608 = root || request.c( 'Spine.Unity.BlendModeMaterials' )
  var i609 = data
  i608.applyAdditiveMaterial = !!i609[0]
  var i611 = i609[1]
  var i610 = new (System.Collections.Generic.List$1(Bridge.ns('Spine.Unity.BlendModeMaterials+ReplacementMaterial')))
  for(var i = 0; i < i611.length; i += 1) {
    i610.add(request.d('Spine.Unity.BlendModeMaterials+ReplacementMaterial', i611[i + 0]));
  }
  i608.additiveMaterials = i610
  var i613 = i609[2]
  var i612 = new (System.Collections.Generic.List$1(Bridge.ns('Spine.Unity.BlendModeMaterials+ReplacementMaterial')))
  for(var i = 0; i < i613.length; i += 1) {
    i612.add(request.d('Spine.Unity.BlendModeMaterials+ReplacementMaterial', i613[i + 0]));
  }
  i608.multiplyMaterials = i612
  var i615 = i609[3]
  var i614 = new (System.Collections.Generic.List$1(Bridge.ns('Spine.Unity.BlendModeMaterials+ReplacementMaterial')))
  for(var i = 0; i < i615.length; i += 1) {
    i614.add(request.d('Spine.Unity.BlendModeMaterials+ReplacementMaterial', i615[i + 0]));
  }
  i608.screenMaterials = i614
  i608.requiresBlendModeMaterials = !!i609[4]
  return i608
}

Deserializers["Spine.Unity.BlendModeMaterials+ReplacementMaterial"] = function (request, data, root) {
  var i618 = root || request.c( 'Spine.Unity.BlendModeMaterials+ReplacementMaterial' )
  var i619 = data
  i618.pageName = i619[0]
  request.r(i619[1], i619[2], 0, i618, 'material')
  return i618
}

Deserializers["Spine.Unity.SpineAtlasAsset"] = function (request, data, root) {
  var i622 = root || request.c( 'Spine.Unity.SpineAtlasAsset' )
  var i623 = data
  request.r(i623[0], i623[1], 0, i622, 'atlasFile')
  var i625 = i623[2]
  var i624 = []
  for(var i = 0; i < i625.length; i += 2) {
  request.r(i625[i + 0], i625[i + 1], 2, i624, '')
  }
  i622.materials = i624
  i622.textureLoadingMode = i623[3]
  request.r(i623[4], i623[5], 0, i622, 'onDemandTextureLoader')
  return i622
}

Deserializers["DG.Tweening.Core.DOTweenSettings"] = function (request, data, root) {
  var i626 = root || request.c( 'DG.Tweening.Core.DOTweenSettings' )
  var i627 = data
  i626.useSafeMode = !!i627[0]
  i626.safeModeOptions = request.d('DG.Tweening.Core.DOTweenSettings+SafeModeOptions', i627[1], i626.safeModeOptions)
  i626.timeScale = i627[2]
  i626.unscaledTimeScale = i627[3]
  i626.useSmoothDeltaTime = !!i627[4]
  i626.maxSmoothUnscaledTime = i627[5]
  i626.rewindCallbackMode = i627[6]
  i626.showUnityEditorReport = !!i627[7]
  i626.logBehaviour = i627[8]
  i626.drawGizmos = !!i627[9]
  i626.defaultRecyclable = !!i627[10]
  i626.defaultAutoPlay = i627[11]
  i626.defaultUpdateType = i627[12]
  i626.defaultTimeScaleIndependent = !!i627[13]
  i626.defaultEaseType = i627[14]
  i626.defaultEaseOvershootOrAmplitude = i627[15]
  i626.defaultEasePeriod = i627[16]
  i626.defaultAutoKill = !!i627[17]
  i626.defaultLoopType = i627[18]
  i626.debugMode = !!i627[19]
  i626.debugStoreTargetId = !!i627[20]
  i626.showPreviewPanel = !!i627[21]
  i626.storeSettingsLocation = i627[22]
  i626.modules = request.d('DG.Tweening.Core.DOTweenSettings+ModulesSetup', i627[23], i626.modules)
  i626.createASMDEF = !!i627[24]
  i626.showPlayingTweens = !!i627[25]
  i626.showPausedTweens = !!i627[26]
  return i626
}

Deserializers["DG.Tweening.Core.DOTweenSettings+SafeModeOptions"] = function (request, data, root) {
  var i628 = root || request.c( 'DG.Tweening.Core.DOTweenSettings+SafeModeOptions' )
  var i629 = data
  i628.logBehaviour = i629[0]
  i628.nestedTweenFailureBehaviour = i629[1]
  return i628
}

Deserializers["DG.Tweening.Core.DOTweenSettings+ModulesSetup"] = function (request, data, root) {
  var i630 = root || request.c( 'DG.Tweening.Core.DOTweenSettings+ModulesSetup' )
  var i631 = data
  i630.showPanel = !!i631[0]
  i630.audioEnabled = !!i631[1]
  i630.physicsEnabled = !!i631[2]
  i630.physics2DEnabled = !!i631[3]
  i630.spriteEnabled = !!i631[4]
  i630.uiEnabled = !!i631[5]
  i630.uiToolkitEnabled = !!i631[6]
  i630.textMeshProEnabled = !!i631[7]
  i630.tk2DEnabled = !!i631[8]
  i630.deAudioEnabled = !!i631[9]
  i630.deUnityExtendedEnabled = !!i631[10]
  i630.epoOutlineEnabled = !!i631[11]
  return i630
}

Deserializers["TMPro.TMP_Settings"] = function (request, data, root) {
  var i632 = root || request.c( 'TMPro.TMP_Settings' )
  var i633 = data
  i632.m_enableWordWrapping = !!i633[0]
  i632.m_enableKerning = !!i633[1]
  i632.m_enableExtraPadding = !!i633[2]
  i632.m_enableTintAllSprites = !!i633[3]
  i632.m_enableParseEscapeCharacters = !!i633[4]
  i632.m_EnableRaycastTarget = !!i633[5]
  i632.m_GetFontFeaturesAtRuntime = !!i633[6]
  i632.m_missingGlyphCharacter = i633[7]
  i632.m_warningsDisabled = !!i633[8]
  request.r(i633[9], i633[10], 0, i632, 'm_defaultFontAsset')
  i632.m_defaultFontAssetPath = i633[11]
  i632.m_defaultFontSize = i633[12]
  i632.m_defaultAutoSizeMinRatio = i633[13]
  i632.m_defaultAutoSizeMaxRatio = i633[14]
  i632.m_defaultTextMeshProTextContainerSize = new pc.Vec2( i633[15], i633[16] )
  i632.m_defaultTextMeshProUITextContainerSize = new pc.Vec2( i633[17], i633[18] )
  i632.m_autoSizeTextContainer = !!i633[19]
  i632.m_IsTextObjectScaleStatic = !!i633[20]
  var i635 = i633[21]
  var i634 = new (System.Collections.Generic.List$1(Bridge.ns('TMPro.TMP_FontAsset')))
  for(var i = 0; i < i635.length; i += 2) {
  request.r(i635[i + 0], i635[i + 1], 1, i634, '')
  }
  i632.m_fallbackFontAssets = i634
  i632.m_matchMaterialPreset = !!i633[22]
  request.r(i633[23], i633[24], 0, i632, 'm_defaultSpriteAsset')
  i632.m_defaultSpriteAssetPath = i633[25]
  i632.m_enableEmojiSupport = !!i633[26]
  i632.m_MissingCharacterSpriteUnicode = i633[27]
  i632.m_defaultColorGradientPresetsPath = i633[28]
  request.r(i633[29], i633[30], 0, i632, 'm_defaultStyleSheet')
  i632.m_StyleSheetsResourcePath = i633[31]
  request.r(i633[32], i633[33], 0, i632, 'm_leadingCharacters')
  request.r(i633[34], i633[35], 0, i632, 'm_followingCharacters')
  i632.m_UseModernHangulLineBreakingRules = !!i633[36]
  return i632
}

Deserializers["TMPro.TMP_FontAsset"] = function (request, data, root) {
  var i638 = root || request.c( 'TMPro.TMP_FontAsset' )
  var i639 = data
  request.r(i639[0], i639[1], 0, i638, 'atlas')
  i638.normalStyle = i639[2]
  i638.normalSpacingOffset = i639[3]
  i638.boldStyle = i639[4]
  i638.boldSpacing = i639[5]
  i638.italicStyle = i639[6]
  i638.tabSize = i639[7]
  i638.hashCode = i639[8]
  request.r(i639[9], i639[10], 0, i638, 'material')
  i638.materialHashCode = i639[11]
  i638.m_Version = i639[12]
  i638.m_SourceFontFileGUID = i639[13]
  request.r(i639[14], i639[15], 0, i638, 'm_SourceFontFile_EditorRef')
  request.r(i639[16], i639[17], 0, i638, 'm_SourceFontFile')
  i638.m_AtlasPopulationMode = i639[18]
  i638.m_FaceInfo = request.d('UnityEngine.TextCore.FaceInfo', i639[19], i638.m_FaceInfo)
  var i641 = i639[20]
  var i640 = new (System.Collections.Generic.List$1(Bridge.ns('UnityEngine.TextCore.Glyph')))
  for(var i = 0; i < i641.length; i += 1) {
    i640.add(request.d('UnityEngine.TextCore.Glyph', i641[i + 0]));
  }
  i638.m_GlyphTable = i640
  var i643 = i639[21]
  var i642 = new (System.Collections.Generic.List$1(Bridge.ns('TMPro.TMP_Character')))
  for(var i = 0; i < i643.length; i += 1) {
    i642.add(request.d('TMPro.TMP_Character', i643[i + 0]));
  }
  i638.m_CharacterTable = i642
  var i645 = i639[22]
  var i644 = []
  for(var i = 0; i < i645.length; i += 2) {
  request.r(i645[i + 0], i645[i + 1], 2, i644, '')
  }
  i638.m_AtlasTextures = i644
  i638.m_AtlasTextureIndex = i639[23]
  i638.m_IsMultiAtlasTexturesEnabled = !!i639[24]
  i638.m_ClearDynamicDataOnBuild = !!i639[25]
  var i647 = i639[26]
  var i646 = new (System.Collections.Generic.List$1(Bridge.ns('UnityEngine.TextCore.GlyphRect')))
  for(var i = 0; i < i647.length; i += 1) {
    i646.add(request.d('UnityEngine.TextCore.GlyphRect', i647[i + 0]));
  }
  i638.m_UsedGlyphRects = i646
  var i649 = i639[27]
  var i648 = new (System.Collections.Generic.List$1(Bridge.ns('UnityEngine.TextCore.GlyphRect')))
  for(var i = 0; i < i649.length; i += 1) {
    i648.add(request.d('UnityEngine.TextCore.GlyphRect', i649[i + 0]));
  }
  i638.m_FreeGlyphRects = i648
  i638.m_fontInfo = request.d('TMPro.FaceInfo_Legacy', i639[28], i638.m_fontInfo)
  i638.m_AtlasWidth = i639[29]
  i638.m_AtlasHeight = i639[30]
  i638.m_AtlasPadding = i639[31]
  i638.m_AtlasRenderMode = i639[32]
  var i651 = i639[33]
  var i650 = new (System.Collections.Generic.List$1(Bridge.ns('TMPro.TMP_Glyph')))
  for(var i = 0; i < i651.length; i += 1) {
    i650.add(request.d('TMPro.TMP_Glyph', i651[i + 0]));
  }
  i638.m_glyphInfoList = i650
  i638.m_KerningTable = request.d('TMPro.KerningTable', i639[34], i638.m_KerningTable)
  i638.m_FontFeatureTable = request.d('TMPro.TMP_FontFeatureTable', i639[35], i638.m_FontFeatureTable)
  var i653 = i639[36]
  var i652 = new (System.Collections.Generic.List$1(Bridge.ns('TMPro.TMP_FontAsset')))
  for(var i = 0; i < i653.length; i += 2) {
  request.r(i653[i + 0], i653[i + 1], 1, i652, '')
  }
  i638.fallbackFontAssets = i652
  var i655 = i639[37]
  var i654 = new (System.Collections.Generic.List$1(Bridge.ns('TMPro.TMP_FontAsset')))
  for(var i = 0; i < i655.length; i += 2) {
  request.r(i655[i + 0], i655[i + 1], 1, i654, '')
  }
  i638.m_FallbackFontAssetTable = i654
  i638.m_CreationSettings = request.d('TMPro.FontAssetCreationSettings', i639[38], i638.m_CreationSettings)
  var i657 = i639[39]
  var i656 = []
  for(var i = 0; i < i657.length; i += 1) {
    i656.push( request.d('TMPro.TMP_FontWeightPair', i657[i + 0]) );
  }
  i638.m_FontWeightTable = i656
  var i659 = i639[40]
  var i658 = []
  for(var i = 0; i < i659.length; i += 1) {
    i658.push( request.d('TMPro.TMP_FontWeightPair', i659[i + 0]) );
  }
  i638.fontWeights = i658
  return i638
}

Deserializers["UnityEngine.TextCore.FaceInfo"] = function (request, data, root) {
  var i660 = root || request.c( 'UnityEngine.TextCore.FaceInfo' )
  var i661 = data
  i660.m_FaceIndex = i661[0]
  i660.m_FamilyName = i661[1]
  i660.m_StyleName = i661[2]
  i660.m_PointSize = i661[3]
  i660.m_Scale = i661[4]
  i660.m_UnitsPerEM = i661[5]
  i660.m_LineHeight = i661[6]
  i660.m_AscentLine = i661[7]
  i660.m_CapLine = i661[8]
  i660.m_MeanLine = i661[9]
  i660.m_Baseline = i661[10]
  i660.m_DescentLine = i661[11]
  i660.m_SuperscriptOffset = i661[12]
  i660.m_SuperscriptSize = i661[13]
  i660.m_SubscriptOffset = i661[14]
  i660.m_SubscriptSize = i661[15]
  i660.m_UnderlineOffset = i661[16]
  i660.m_UnderlineThickness = i661[17]
  i660.m_StrikethroughOffset = i661[18]
  i660.m_StrikethroughThickness = i661[19]
  i660.m_TabWidth = i661[20]
  return i660
}

Deserializers["UnityEngine.TextCore.Glyph"] = function (request, data, root) {
  var i664 = root || request.c( 'UnityEngine.TextCore.Glyph' )
  var i665 = data
  i664.m_Index = i665[0]
  i664.m_Metrics = request.d('UnityEngine.TextCore.GlyphMetrics', i665[1], i664.m_Metrics)
  i664.m_GlyphRect = request.d('UnityEngine.TextCore.GlyphRect', i665[2], i664.m_GlyphRect)
  i664.m_Scale = i665[3]
  i664.m_AtlasIndex = i665[4]
  i664.m_ClassDefinitionType = i665[5]
  return i664
}

Deserializers["UnityEngine.TextCore.GlyphMetrics"] = function (request, data, root) {
  var i666 = root || request.c( 'UnityEngine.TextCore.GlyphMetrics' )
  var i667 = data
  i666.m_Width = i667[0]
  i666.m_Height = i667[1]
  i666.m_HorizontalBearingX = i667[2]
  i666.m_HorizontalBearingY = i667[3]
  i666.m_HorizontalAdvance = i667[4]
  return i666
}

Deserializers["UnityEngine.TextCore.GlyphRect"] = function (request, data, root) {
  var i668 = root || request.c( 'UnityEngine.TextCore.GlyphRect' )
  var i669 = data
  i668.m_X = i669[0]
  i668.m_Y = i669[1]
  i668.m_Width = i669[2]
  i668.m_Height = i669[3]
  return i668
}

Deserializers["TMPro.TMP_Character"] = function (request, data, root) {
  var i672 = root || request.c( 'TMPro.TMP_Character' )
  var i673 = data
  i672.m_ElementType = i673[0]
  i672.m_Unicode = i673[1]
  i672.m_GlyphIndex = i673[2]
  i672.m_Scale = i673[3]
  return i672
}

Deserializers["TMPro.FaceInfo_Legacy"] = function (request, data, root) {
  var i678 = root || request.c( 'TMPro.FaceInfo_Legacy' )
  var i679 = data
  i678.Name = i679[0]
  i678.PointSize = i679[1]
  i678.Scale = i679[2]
  i678.CharacterCount = i679[3]
  i678.LineHeight = i679[4]
  i678.Baseline = i679[5]
  i678.Ascender = i679[6]
  i678.CapHeight = i679[7]
  i678.Descender = i679[8]
  i678.CenterLine = i679[9]
  i678.SuperscriptOffset = i679[10]
  i678.SubscriptOffset = i679[11]
  i678.SubSize = i679[12]
  i678.Underline = i679[13]
  i678.UnderlineThickness = i679[14]
  i678.strikethrough = i679[15]
  i678.strikethroughThickness = i679[16]
  i678.TabWidth = i679[17]
  i678.Padding = i679[18]
  i678.AtlasWidth = i679[19]
  i678.AtlasHeight = i679[20]
  return i678
}

Deserializers["TMPro.TMP_Glyph"] = function (request, data, root) {
  var i682 = root || request.c( 'TMPro.TMP_Glyph' )
  var i683 = data
  i682.id = i683[0]
  i682.x = i683[1]
  i682.y = i683[2]
  i682.width = i683[3]
  i682.height = i683[4]
  i682.xOffset = i683[5]
  i682.yOffset = i683[6]
  i682.xAdvance = i683[7]
  i682.scale = i683[8]
  return i682
}

Deserializers["TMPro.KerningTable"] = function (request, data, root) {
  var i684 = root || request.c( 'TMPro.KerningTable' )
  var i685 = data
  var i687 = i685[0]
  var i686 = new (System.Collections.Generic.List$1(Bridge.ns('TMPro.KerningPair')))
  for(var i = 0; i < i687.length; i += 1) {
    i686.add(request.d('TMPro.KerningPair', i687[i + 0]));
  }
  i684.kerningPairs = i686
  return i684
}

Deserializers["TMPro.KerningPair"] = function (request, data, root) {
  var i690 = root || request.c( 'TMPro.KerningPair' )
  var i691 = data
  i690.xOffset = i691[0]
  i690.m_FirstGlyph = i691[1]
  i690.m_FirstGlyphAdjustments = request.d('TMPro.GlyphValueRecord_Legacy', i691[2], i690.m_FirstGlyphAdjustments)
  i690.m_SecondGlyph = i691[3]
  i690.m_SecondGlyphAdjustments = request.d('TMPro.GlyphValueRecord_Legacy', i691[4], i690.m_SecondGlyphAdjustments)
  i690.m_IgnoreSpacingAdjustments = !!i691[5]
  return i690
}

Deserializers["TMPro.TMP_FontFeatureTable"] = function (request, data, root) {
  var i692 = root || request.c( 'TMPro.TMP_FontFeatureTable' )
  var i693 = data
  var i695 = i693[0]
  var i694 = new (System.Collections.Generic.List$1(Bridge.ns('TMPro.TMP_GlyphPairAdjustmentRecord')))
  for(var i = 0; i < i695.length; i += 1) {
    i694.add(request.d('TMPro.TMP_GlyphPairAdjustmentRecord', i695[i + 0]));
  }
  i692.m_GlyphPairAdjustmentRecords = i694
  return i692
}

Deserializers["TMPro.TMP_GlyphPairAdjustmentRecord"] = function (request, data, root) {
  var i698 = root || request.c( 'TMPro.TMP_GlyphPairAdjustmentRecord' )
  var i699 = data
  i698.m_FirstAdjustmentRecord = request.d('TMPro.TMP_GlyphAdjustmentRecord', i699[0], i698.m_FirstAdjustmentRecord)
  i698.m_SecondAdjustmentRecord = request.d('TMPro.TMP_GlyphAdjustmentRecord', i699[1], i698.m_SecondAdjustmentRecord)
  i698.m_FeatureLookupFlags = i699[2]
  return i698
}

Deserializers["TMPro.TMP_GlyphAdjustmentRecord"] = function (request, data, root) {
  var i700 = root || request.c( 'TMPro.TMP_GlyphAdjustmentRecord' )
  var i701 = data
  i700.m_GlyphIndex = i701[0]
  i700.m_GlyphValueRecord = request.d('TMPro.TMP_GlyphValueRecord', i701[1], i700.m_GlyphValueRecord)
  return i700
}

Deserializers["TMPro.TMP_GlyphValueRecord"] = function (request, data, root) {
  var i702 = root || request.c( 'TMPro.TMP_GlyphValueRecord' )
  var i703 = data
  i702.m_XPlacement = i703[0]
  i702.m_YPlacement = i703[1]
  i702.m_XAdvance = i703[2]
  i702.m_YAdvance = i703[3]
  return i702
}

Deserializers["TMPro.FontAssetCreationSettings"] = function (request, data, root) {
  var i704 = root || request.c( 'TMPro.FontAssetCreationSettings' )
  var i705 = data
  i704.sourceFontFileName = i705[0]
  i704.sourceFontFileGUID = i705[1]
  i704.pointSizeSamplingMode = i705[2]
  i704.pointSize = i705[3]
  i704.padding = i705[4]
  i704.packingMode = i705[5]
  i704.atlasWidth = i705[6]
  i704.atlasHeight = i705[7]
  i704.characterSetSelectionMode = i705[8]
  i704.characterSequence = i705[9]
  i704.referencedFontAssetGUID = i705[10]
  i704.referencedTextAssetGUID = i705[11]
  i704.fontStyle = i705[12]
  i704.fontStyleModifier = i705[13]
  i704.renderMode = i705[14]
  i704.includeFontFeatures = !!i705[15]
  return i704
}

Deserializers["TMPro.TMP_FontWeightPair"] = function (request, data, root) {
  var i708 = root || request.c( 'TMPro.TMP_FontWeightPair' )
  var i709 = data
  request.r(i709[0], i709[1], 0, i708, 'regularTypeface')
  request.r(i709[2], i709[3], 0, i708, 'italicTypeface')
  return i708
}

Deserializers["TMPro.TMP_SpriteAsset"] = function (request, data, root) {
  var i710 = root || request.c( 'TMPro.TMP_SpriteAsset' )
  var i711 = data
  request.r(i711[0], i711[1], 0, i710, 'spriteSheet')
  var i713 = i711[2]
  var i712 = new (System.Collections.Generic.List$1(Bridge.ns('TMPro.TMP_Sprite')))
  for(var i = 0; i < i713.length; i += 1) {
    i712.add(request.d('TMPro.TMP_Sprite', i713[i + 0]));
  }
  i710.spriteInfoList = i712
  var i715 = i711[3]
  var i714 = new (System.Collections.Generic.List$1(Bridge.ns('TMPro.TMP_SpriteAsset')))
  for(var i = 0; i < i715.length; i += 2) {
  request.r(i715[i + 0], i715[i + 1], 1, i714, '')
  }
  i710.fallbackSpriteAssets = i714
  i710.hashCode = i711[4]
  request.r(i711[5], i711[6], 0, i710, 'material')
  i710.materialHashCode = i711[7]
  i710.m_Version = i711[8]
  i710.m_FaceInfo = request.d('UnityEngine.TextCore.FaceInfo', i711[9], i710.m_FaceInfo)
  var i717 = i711[10]
  var i716 = new (System.Collections.Generic.List$1(Bridge.ns('TMPro.TMP_SpriteCharacter')))
  for(var i = 0; i < i717.length; i += 1) {
    i716.add(request.d('TMPro.TMP_SpriteCharacter', i717[i + 0]));
  }
  i710.m_SpriteCharacterTable = i716
  var i719 = i711[11]
  var i718 = new (System.Collections.Generic.List$1(Bridge.ns('TMPro.TMP_SpriteGlyph')))
  for(var i = 0; i < i719.length; i += 1) {
    i718.add(request.d('TMPro.TMP_SpriteGlyph', i719[i + 0]));
  }
  i710.m_SpriteGlyphTable = i718
  return i710
}

Deserializers["TMPro.TMP_Sprite"] = function (request, data, root) {
  var i722 = root || request.c( 'TMPro.TMP_Sprite' )
  var i723 = data
  i722.name = i723[0]
  i722.hashCode = i723[1]
  i722.unicode = i723[2]
  i722.pivot = new pc.Vec2( i723[3], i723[4] )
  request.r(i723[5], i723[6], 0, i722, 'sprite')
  i722.id = i723[7]
  i722.x = i723[8]
  i722.y = i723[9]
  i722.width = i723[10]
  i722.height = i723[11]
  i722.xOffset = i723[12]
  i722.yOffset = i723[13]
  i722.xAdvance = i723[14]
  i722.scale = i723[15]
  return i722
}

Deserializers["TMPro.TMP_SpriteCharacter"] = function (request, data, root) {
  var i728 = root || request.c( 'TMPro.TMP_SpriteCharacter' )
  var i729 = data
  i728.m_Name = i729[0]
  i728.m_HashCode = i729[1]
  i728.m_ElementType = i729[2]
  i728.m_Unicode = i729[3]
  i728.m_GlyphIndex = i729[4]
  i728.m_Scale = i729[5]
  return i728
}

Deserializers["TMPro.TMP_SpriteGlyph"] = function (request, data, root) {
  var i732 = root || request.c( 'TMPro.TMP_SpriteGlyph' )
  var i733 = data
  request.r(i733[0], i733[1], 0, i732, 'sprite')
  i732.m_Index = i733[2]
  i732.m_Metrics = request.d('UnityEngine.TextCore.GlyphMetrics', i733[3], i732.m_Metrics)
  i732.m_GlyphRect = request.d('UnityEngine.TextCore.GlyphRect', i733[4], i732.m_GlyphRect)
  i732.m_Scale = i733[5]
  i732.m_AtlasIndex = i733[6]
  i732.m_ClassDefinitionType = i733[7]
  return i732
}

Deserializers["TMPro.TMP_StyleSheet"] = function (request, data, root) {
  var i734 = root || request.c( 'TMPro.TMP_StyleSheet' )
  var i735 = data
  var i737 = i735[0]
  var i736 = new (System.Collections.Generic.List$1(Bridge.ns('TMPro.TMP_Style')))
  for(var i = 0; i < i737.length; i += 1) {
    i736.add(request.d('TMPro.TMP_Style', i737[i + 0]));
  }
  i734.m_StyleList = i736
  return i734
}

Deserializers["TMPro.TMP_Style"] = function (request, data, root) {
  var i740 = root || request.c( 'TMPro.TMP_Style' )
  var i741 = data
  i740.m_Name = i741[0]
  i740.m_HashCode = i741[1]
  i740.m_OpeningDefinition = i741[2]
  i740.m_ClosingDefinition = i741[3]
  i740.m_OpeningTagArray = i741[4]
  i740.m_ClosingTagArray = i741[5]
  i740.m_OpeningTagUnicodeArray = i741[6]
  i740.m_ClosingTagUnicodeArray = i741[7]
  return i740
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Resources"] = function (request, data, root) {
  var i742 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Resources' )
  var i743 = data
  var i745 = i743[0]
  var i744 = []
  for(var i = 0; i < i745.length; i += 1) {
    i744.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.Resources+File', i745[i + 0]) );
  }
  i742.files = i744
  i742.componentToPrefabIds = i743[1]
  return i742
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Resources+File"] = function (request, data, root) {
  var i748 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Resources+File' )
  var i749 = data
  i748.path = i749[0]
  request.r(i749[1], i749[2], 0, i748, 'unityObject')
  return i748
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings"] = function (request, data, root) {
  var i750 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings' )
  var i751 = data
  var i753 = i751[0]
  var i752 = []
  for(var i = 0; i < i753.length; i += 1) {
    i752.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+ScriptsExecutionOrder', i753[i + 0]) );
  }
  i750.scriptsExecutionOrder = i752
  var i755 = i751[1]
  var i754 = []
  for(var i = 0; i < i755.length; i += 1) {
    i754.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+SortingLayer', i755[i + 0]) );
  }
  i750.sortingLayers = i754
  var i757 = i751[2]
  var i756 = []
  for(var i = 0; i < i757.length; i += 1) {
    i756.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+CullingLayer', i757[i + 0]) );
  }
  i750.cullingLayers = i756
  i750.timeSettings = request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+TimeSettings', i751[3], i750.timeSettings)
  i750.physicsSettings = request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings', i751[4], i750.physicsSettings)
  i750.physics2DSettings = request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings', i751[5], i750.physics2DSettings)
  i750.qualitySettings = request.d('Luna.Unity.DTO.UnityEngine.Assets.QualitySettings', i751[6], i750.qualitySettings)
  i750.enableRealtimeShadows = !!i751[7]
  i750.enableAutoInstancing = !!i751[8]
  i750.enableStaticBatching = !!i751[9]
  i750.enableDynamicBatching = !!i751[10]
  i750.usePreservativeDynamicBatching = !!i751[11]
  i750.lightmapEncodingQuality = i751[12]
  i750.desiredColorSpace = i751[13]
  var i759 = i751[14]
  var i758 = []
  for(var i = 0; i < i759.length; i += 1) {
    i758.push( i759[i + 0] );
  }
  i750.allTags = i758
  return i750
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+ScriptsExecutionOrder"] = function (request, data, root) {
  var i762 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+ScriptsExecutionOrder' )
  var i763 = data
  i762.name = i763[0]
  i762.value = i763[1]
  return i762
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+SortingLayer"] = function (request, data, root) {
  var i766 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+SortingLayer' )
  var i767 = data
  i766.id = i767[0]
  i766.name = i767[1]
  i766.value = i767[2]
  return i766
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+CullingLayer"] = function (request, data, root) {
  var i770 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+CullingLayer' )
  var i771 = data
  i770.id = i771[0]
  i770.name = i771[1]
  return i770
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+TimeSettings"] = function (request, data, root) {
  var i772 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+TimeSettings' )
  var i773 = data
  i772.fixedDeltaTime = i773[0]
  i772.maximumDeltaTime = i773[1]
  i772.timeScale = i773[2]
  i772.maximumParticleTimestep = i773[3]
  return i772
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings"] = function (request, data, root) {
  var i774 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings' )
  var i775 = data
  i774.gravity = new pc.Vec3( i775[0], i775[1], i775[2] )
  i774.defaultSolverIterations = i775[3]
  i774.bounceThreshold = i775[4]
  i774.autoSyncTransforms = !!i775[5]
  i774.autoSimulation = !!i775[6]
  var i777 = i775[7]
  var i776 = []
  for(var i = 0; i < i777.length; i += 1) {
    i776.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings+CollisionMask', i777[i + 0]) );
  }
  i774.collisionMatrix = i776
  return i774
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings+CollisionMask"] = function (request, data, root) {
  var i780 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings+CollisionMask' )
  var i781 = data
  i780.enabled = !!i781[0]
  i780.layerId = i781[1]
  i780.otherLayerId = i781[2]
  return i780
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings"] = function (request, data, root) {
  var i782 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings' )
  var i783 = data
  request.r(i783[0], i783[1], 0, i782, 'material')
  i782.gravity = new pc.Vec2( i783[2], i783[3] )
  i782.positionIterations = i783[4]
  i782.velocityIterations = i783[5]
  i782.velocityThreshold = i783[6]
  i782.maxLinearCorrection = i783[7]
  i782.maxAngularCorrection = i783[8]
  i782.maxTranslationSpeed = i783[9]
  i782.maxRotationSpeed = i783[10]
  i782.baumgarteScale = i783[11]
  i782.baumgarteTOIScale = i783[12]
  i782.timeToSleep = i783[13]
  i782.linearSleepTolerance = i783[14]
  i782.angularSleepTolerance = i783[15]
  i782.defaultContactOffset = i783[16]
  i782.autoSimulation = !!i783[17]
  i782.queriesHitTriggers = !!i783[18]
  i782.queriesStartInColliders = !!i783[19]
  i782.callbacksOnDisable = !!i783[20]
  i782.reuseCollisionCallbacks = !!i783[21]
  i782.autoSyncTransforms = !!i783[22]
  var i785 = i783[23]
  var i784 = []
  for(var i = 0; i < i785.length; i += 1) {
    i784.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings+CollisionMask', i785[i + 0]) );
  }
  i782.collisionMatrix = i784
  return i782
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings+CollisionMask"] = function (request, data, root) {
  var i788 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings+CollisionMask' )
  var i789 = data
  i788.enabled = !!i789[0]
  i788.layerId = i789[1]
  i788.otherLayerId = i789[2]
  return i788
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.QualitySettings"] = function (request, data, root) {
  var i790 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.QualitySettings' )
  var i791 = data
  var i793 = i791[0]
  var i792 = []
  for(var i = 0; i < i793.length; i += 1) {
    i792.push( request.d('Luna.Unity.DTO.UnityEngine.Assets.QualitySettings', i793[i + 0]) );
  }
  i790.qualityLevels = i792
  var i795 = i791[1]
  var i794 = []
  for(var i = 0; i < i795.length; i += 1) {
    i794.push( i795[i + 0] );
  }
  i790.names = i794
  i790.shadows = i791[2]
  i790.anisotropicFiltering = i791[3]
  i790.antiAliasing = i791[4]
  i790.lodBias = i791[5]
  i790.shadowCascades = i791[6]
  i790.shadowDistance = i791[7]
  i790.shadowmaskMode = i791[8]
  i790.shadowProjection = i791[9]
  i790.shadowResolution = i791[10]
  i790.softParticles = !!i791[11]
  i790.softVegetation = !!i791[12]
  i790.activeColorSpace = i791[13]
  i790.desiredColorSpace = i791[14]
  i790.masterTextureLimit = i791[15]
  i790.maxQueuedFrames = i791[16]
  i790.particleRaycastBudget = i791[17]
  i790.pixelLightCount = i791[18]
  i790.realtimeReflectionProbes = !!i791[19]
  i790.shadowCascade2Split = i791[20]
  i790.shadowCascade4Split = new pc.Vec3( i791[21], i791[22], i791[23] )
  i790.streamingMipmapsActive = !!i791[24]
  i790.vSyncCount = i791[25]
  i790.asyncUploadBufferSize = i791[26]
  i790.asyncUploadTimeSlice = i791[27]
  i790.billboardsFaceCameraPosition = !!i791[28]
  i790.shadowNearPlaneOffset = i791[29]
  i790.streamingMipmapsMemoryBudget = i791[30]
  i790.maximumLODLevel = i791[31]
  i790.streamingMipmapsAddAllCameras = !!i791[32]
  i790.streamingMipmapsMaxLevelReduction = i791[33]
  i790.streamingMipmapsRenderersPerFrame = i791[34]
  i790.resolutionScalingFixedDPIFactor = i791[35]
  i790.streamingMipmapsMaxFileIORequests = i791[36]
  i790.currentQualityLevel = i791[37]
  return i790
}

Deserializers["Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShapeFrame"] = function (request, data, root) {
  var i800 = root || request.c( 'Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShapeFrame' )
  var i801 = data
  i800.weight = i801[0]
  i800.vertices = i801[1]
  i800.normals = i801[2]
  i800.tangents = i801[3]
  return i800
}

Deserializers["TMPro.GlyphValueRecord_Legacy"] = function (request, data, root) {
  var i802 = root || request.c( 'TMPro.GlyphValueRecord_Legacy' )
  var i803 = data
  i802.xPlacement = i803[0]
  i802.yPlacement = i803[1]
  i802.xAdvance = i803[2]
  i802.yAdvance = i803[3]
  return i802
}

Deserializers.fields = {"Luna.Unity.DTO.UnityEngine.Assets.Material":{"name":0,"shader":1,"renderQueue":3,"enableInstancing":4,"floatParameters":5,"colorParameters":6,"vectorParameters":7,"textureParameters":8,"materialFlags":9},"Luna.Unity.DTO.UnityEngine.Assets.Material+FloatParameter":{"name":0,"value":1},"Luna.Unity.DTO.UnityEngine.Assets.Material+ColorParameter":{"name":0,"value":1},"Luna.Unity.DTO.UnityEngine.Assets.Material+VectorParameter":{"name":0,"value":1},"Luna.Unity.DTO.UnityEngine.Assets.Material+TextureParameter":{"name":0,"value":1},"Luna.Unity.DTO.UnityEngine.Assets.Material+MaterialFlag":{"name":0,"enabled":1},"Luna.Unity.DTO.UnityEngine.Textures.Texture2D":{"name":0,"width":1,"height":2,"mipmapCount":3,"anisoLevel":4,"filterMode":5,"hdr":6,"format":7,"wrapMode":8,"alphaIsTransparency":9,"alphaSource":10,"graphicsFormat":11,"sRGBTexture":12,"desiredColorSpace":13,"wrapU":14,"wrapV":15},"Luna.Unity.DTO.UnityEngine.Assets.Mesh":{"name":0,"halfPrecision":1,"useSimplification":2,"useUInt32IndexFormat":3,"vertexCount":4,"aabb":5,"streams":6,"vertices":7,"subMeshes":8,"bindposes":9,"blendShapes":10},"Luna.Unity.DTO.UnityEngine.Assets.Mesh+SubMesh":{"triangles":0},"Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShape":{"name":0,"frames":1},"Luna.Unity.DTO.UnityEngine.Scene.Scene":{"name":0,"index":1,"startup":2},"Luna.Unity.DTO.UnityEngine.Components.Camera":{"aspect":0,"orthographic":1,"orthographicSize":2,"backgroundColor":3,"nearClipPlane":7,"farClipPlane":8,"fieldOfView":9,"depth":10,"clearFlags":11,"cullingMask":12,"rect":13,"targetTexture":14,"usePhysicalProperties":16,"focalLength":17,"sensorSize":18,"lensShift":20,"gateFit":22,"commandBufferCount":23,"cameraType":24,"enabled":25},"Luna.Unity.DTO.UnityEngine.Scene.GameObject":{"name":0,"tagId":1,"enabled":2,"isStatic":3,"layer":4},"Luna.Unity.DTO.UnityEngine.Components.MeshRenderer":{"additionalVertexStreams":0,"enabled":2,"sharedMaterial":3,"sharedMaterials":5,"receiveShadows":6,"shadowCastingMode":7,"sortingLayerID":8,"sortingOrder":9,"lightmapIndex":10,"lightmapSceneIndex":11,"lightmapScaleOffset":12,"lightProbeUsage":16,"reflectionProbeUsage":17},"Luna.Unity.DTO.UnityEngine.Components.MeshFilter":{"sharedMesh":0},"Luna.Unity.DTO.UnityEngine.Assets.RenderSettings":{"ambientIntensity":0,"reflectionIntensity":1,"ambientMode":2,"ambientLight":3,"ambientSkyColor":7,"ambientGroundColor":11,"ambientEquatorColor":15,"fogColor":19,"fogEndDistance":23,"fogStartDistance":24,"fogDensity":25,"fog":26,"skybox":27,"fogMode":29,"lightmaps":30,"lightProbes":31,"lightmapsMode":32,"mixedBakeMode":33,"environmentLightingMode":34,"ambientProbe":35,"customReflection":36,"defaultReflection":38,"defaultReflectionMode":40,"defaultReflectionResolution":41,"sunLightObjectId":42,"pixelLightCount":43,"defaultReflectionHDR":44,"hasLightDataAsset":45,"hasManualGenerate":46},"Luna.Unity.DTO.UnityEngine.Assets.RenderSettings+Lightmap":{"lightmapColor":0,"lightmapDirection":2,"shadowMask":4},"Luna.Unity.DTO.UnityEngine.Assets.RenderSettings+LightProbes":{"bakedProbes":0,"positions":1,"hullRays":2,"tetrahedra":3,"neighbours":4,"matrices":5},"Luna.Unity.DTO.UnityEngine.Assets.Shader":{"ShaderCompilationErrors":0,"name":1,"guid":2,"shaderDefinedKeywords":3,"passes":4,"usePasses":5,"defaultParameterValues":6,"unityFallbackShader":7,"readDepth":9,"hasDepthOnlyPass":10,"isCreatedByShaderGraph":11,"disableBatching":12,"compiled":13},"Luna.Unity.DTO.UnityEngine.Assets.Shader+ShaderCompilationError":{"shaderName":0,"errorMessage":1},"Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass":{"id":0,"subShaderIndex":1,"name":2,"passType":3,"grabPassTextureName":4,"usePass":5,"zTest":6,"zWrite":7,"culling":8,"blending":9,"alphaBlending":10,"colorWriteMask":11,"offsetUnits":12,"offsetFactor":13,"stencilRef":14,"stencilReadMask":15,"stencilWriteMask":16,"stencilOp":17,"stencilOpFront":18,"stencilOpBack":19,"tags":20,"passDefinedKeywords":21,"passDefinedKeywordGroups":22,"variants":23,"excludedVariants":24,"hasDepthReader":25},"Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Value":{"val":0,"name":1},"Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Blending":{"src":0,"dst":1,"op":2},"Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+StencilOp":{"pass":0,"fail":1,"zFail":2,"comp":3},"Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Tag":{"name":0,"value":1},"Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+KeywordGroup":{"keywords":0,"hasDiscard":1},"Luna.Unity.DTO.UnityEngine.Assets.Shader+Pass+Variant":{"passId":0,"subShaderIndex":1,"keywords":2,"vertexProgram":3,"fragmentProgram":4,"exportedForWebGl2":5,"readDepth":6},"Luna.Unity.DTO.UnityEngine.Assets.Shader+UsePass":{"shader":0,"pass":2},"Luna.Unity.DTO.UnityEngine.Assets.Shader+DefaultParameterValue":{"name":0,"type":1,"value":2,"textureValue":6,"shaderPropertyFlag":7},"Luna.Unity.DTO.UnityEngine.Assets.Font":{"name":0,"ascent":1,"originalLineHeight":2,"fontSize":3,"characterInfo":4,"texture":5,"originalFontSize":7},"Luna.Unity.DTO.UnityEngine.Assets.Font+CharacterInfo":{"index":0,"advance":1,"bearing":2,"glyphWidth":3,"glyphHeight":4,"minX":5,"maxX":6,"minY":7,"maxY":8,"uvBottomLeftX":9,"uvBottomLeftY":10,"uvBottomRightX":11,"uvBottomRightY":12,"uvTopLeftX":13,"uvTopLeftY":14,"uvTopRightX":15,"uvTopRightY":16},"Luna.Unity.DTO.UnityEngine.Assets.TextAsset":{"name":0,"bytes64":1,"data":2},"Luna.Unity.DTO.UnityEngine.Assets.Resources":{"files":0,"componentToPrefabIds":1},"Luna.Unity.DTO.UnityEngine.Assets.Resources+File":{"path":0,"unityObject":1},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings":{"scriptsExecutionOrder":0,"sortingLayers":1,"cullingLayers":2,"timeSettings":3,"physicsSettings":4,"physics2DSettings":5,"qualitySettings":6,"enableRealtimeShadows":7,"enableAutoInstancing":8,"enableStaticBatching":9,"enableDynamicBatching":10,"usePreservativeDynamicBatching":11,"lightmapEncodingQuality":12,"desiredColorSpace":13,"allTags":14},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+ScriptsExecutionOrder":{"name":0,"value":1},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+SortingLayer":{"id":0,"name":1,"value":2},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+CullingLayer":{"id":0,"name":1},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+TimeSettings":{"fixedDeltaTime":0,"maximumDeltaTime":1,"timeScale":2,"maximumParticleTimestep":3},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings":{"gravity":0,"defaultSolverIterations":3,"bounceThreshold":4,"autoSyncTransforms":5,"autoSimulation":6,"collisionMatrix":7},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+PhysicsSettings+CollisionMask":{"enabled":0,"layerId":1,"otherLayerId":2},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings":{"material":0,"gravity":2,"positionIterations":4,"velocityIterations":5,"velocityThreshold":6,"maxLinearCorrection":7,"maxAngularCorrection":8,"maxTranslationSpeed":9,"maxRotationSpeed":10,"baumgarteScale":11,"baumgarteTOIScale":12,"timeToSleep":13,"linearSleepTolerance":14,"angularSleepTolerance":15,"defaultContactOffset":16,"autoSimulation":17,"queriesHitTriggers":18,"queriesStartInColliders":19,"callbacksOnDisable":20,"reuseCollisionCallbacks":21,"autoSyncTransforms":22,"collisionMatrix":23},"Luna.Unity.DTO.UnityEngine.Assets.ProjectSettings+Physics2DSettings+CollisionMask":{"enabled":0,"layerId":1,"otherLayerId":2},"Luna.Unity.DTO.UnityEngine.Assets.QualitySettings":{"qualityLevels":0,"names":1,"shadows":2,"anisotropicFiltering":3,"antiAliasing":4,"lodBias":5,"shadowCascades":6,"shadowDistance":7,"shadowmaskMode":8,"shadowProjection":9,"shadowResolution":10,"softParticles":11,"softVegetation":12,"activeColorSpace":13,"desiredColorSpace":14,"masterTextureLimit":15,"maxQueuedFrames":16,"particleRaycastBudget":17,"pixelLightCount":18,"realtimeReflectionProbes":19,"shadowCascade2Split":20,"shadowCascade4Split":21,"streamingMipmapsActive":24,"vSyncCount":25,"asyncUploadBufferSize":26,"asyncUploadTimeSlice":27,"billboardsFaceCameraPosition":28,"shadowNearPlaneOffset":29,"streamingMipmapsMemoryBudget":30,"maximumLODLevel":31,"streamingMipmapsAddAllCameras":32,"streamingMipmapsMaxLevelReduction":33,"streamingMipmapsRenderersPerFrame":34,"resolutionScalingFixedDPIFactor":35,"streamingMipmapsMaxFileIORequests":36,"currentQualityLevel":37},"Luna.Unity.DTO.UnityEngine.Assets.Mesh+BlendShapeFrame":{"weight":0,"vertices":1,"normals":2,"tangents":3}}

Deserializers.requiredComponents = {"19":[20],"21":[20],"22":[20],"23":[20],"24":[20],"25":[20],"26":[27],"28":[2],"29":[30],"31":[30],"32":[30],"33":[30],"34":[30],"35":[30],"36":[30],"37":[38],"39":[38],"40":[38],"41":[38],"42":[38],"43":[38],"44":[38],"45":[38],"46":[38],"47":[38],"48":[38],"49":[38],"50":[38],"51":[2],"52":[4],"53":[54],"55":[54],"56":[57],"58":[57],"59":[57],"60":[57],"61":[57],"62":[57],"63":[64],"65":[57],"66":[67,57],"7":[4],"68":[67,57],"69":[70,4],"71":[4],"72":[4,9],"73":[30],"74":[38],"75":[64],"76":[77],"78":[79],"80":[2],"81":[82],"83":[57],"84":[4,57],"85":[57,67],"86":[57],"87":[67,57],"88":[4],"89":[67,57],"90":[57],"91":[92],"93":[92],"94":[92],"95":[57],"96":[57],"97":[56],"98":[67,57],"99":[57],"100":[56],"101":[57],"102":[57],"103":[57],"104":[57],"105":[57],"106":[57],"107":[57],"108":[57],"109":[57],"110":[67,57],"111":[57],"112":[57],"113":[57],"114":[57],"115":[67,57],"116":[57],"117":[118],"119":[118],"120":[118],"121":[118],"122":[2],"123":[2]}

Deserializers.types = ["UnityEngine.Shader","UnityEngine.Texture2D","UnityEngine.Camera","UnityEngine.AudioListener","UnityEngine.MeshRenderer","UnityEngine.Material","UnityEngine.MonoBehaviour","Spine.Unity.SkeletonAnimation","Spine.Unity.SkeletonDataAsset","UnityEngine.MeshFilter","UnityEngine.Mesh","Spine.Unity.SpineAtlasAsset","UnityEngine.TextAsset","DG.Tweening.Core.DOTweenSettings","TMPro.TMP_Settings","TMPro.TMP_FontAsset","TMPro.TMP_SpriteAsset","TMPro.TMP_StyleSheet","UnityEngine.Font","UnityEngine.AudioLowPassFilter","UnityEngine.AudioBehaviour","UnityEngine.AudioHighPassFilter","UnityEngine.AudioReverbFilter","UnityEngine.AudioDistortionFilter","UnityEngine.AudioEchoFilter","UnityEngine.AudioChorusFilter","UnityEngine.Cloth","UnityEngine.SkinnedMeshRenderer","UnityEngine.FlareLayer","UnityEngine.ConstantForce","UnityEngine.Rigidbody","UnityEngine.Joint","UnityEngine.HingeJoint","UnityEngine.SpringJoint","UnityEngine.FixedJoint","UnityEngine.CharacterJoint","UnityEngine.ConfigurableJoint","UnityEngine.CompositeCollider2D","UnityEngine.Rigidbody2D","UnityEngine.Joint2D","UnityEngine.AnchoredJoint2D","UnityEngine.SpringJoint2D","UnityEngine.DistanceJoint2D","UnityEngine.FrictionJoint2D","UnityEngine.HingeJoint2D","UnityEngine.RelativeJoint2D","UnityEngine.SliderJoint2D","UnityEngine.TargetJoint2D","UnityEngine.FixedJoint2D","UnityEngine.WheelJoint2D","UnityEngine.ConstantForce2D","UnityEngine.StreamingController","UnityEngine.TextMesh","UnityEngine.Tilemaps.TilemapRenderer","UnityEngine.Tilemaps.Tilemap","UnityEngine.Tilemaps.TilemapCollider2D","UnityEngine.Canvas","UnityEngine.RectTransform","_Playable.Runtime.View.PlayableChoiceButton","_Playable.Runtime.View.PlayableTapTargetView","Amanotes.Core.ButtonScale","Amanotes.MagicTilesCore.UIButtonTouch","Amanotes.MagicTilesCore.UIImageTouch","Spine.Unity.EditorSkeletonPlayer","Spine.Unity.ISkeletonAnimation","Spine.Unity.BoneFollowerGraphic","Spine.Unity.SkeletonSubmeshGraphic","UnityEngine.CanvasRenderer","Spine.Unity.SkeletonGraphic","Spine.Unity.SkeletonMecanim","UnityEngine.Animator","Spine.Unity.SkeletonRenderer","Spine.Unity.SkeletonPartsRenderer","Spine.Unity.FollowLocationRigidbody","Spine.Unity.FollowLocationRigidbody2D","Spine.Unity.SkeletonUtility","Spine.Unity.SkeletonUtilityConstraint","Spine.Unity.SkeletonUtilityBone","UnityEngine.U2D.Animation.SpriteSkin","UnityEngine.SpriteRenderer","UnityEngine.U2D.PixelPerfectCamera","UnityEngine.U2D.SpriteShapeController","UnityEngine.U2D.SpriteShapeRenderer","TMPro.TextContainer","TMPro.TextMeshPro","TMPro.TextMeshProUGUI","TMPro.TMP_Dropdown","TMPro.TMP_SelectionCaret","TMPro.TMP_SubMesh","TMPro.TMP_SubMeshUI","TMPro.TMP_Text","Unity.VisualScripting.SceneVariables","Unity.VisualScripting.Variables","Unity.VisualScripting.ScriptMachine","Unity.VisualScripting.StateMachine","UnityEngine.UI.Dropdown","UnityEngine.UI.Graphic","UnityEngine.UI.GraphicRaycaster","UnityEngine.UI.Image","UnityEngine.UI.AspectRatioFitter","UnityEngine.UI.CanvasScaler","UnityEngine.UI.ContentSizeFitter","UnityEngine.UI.GridLayoutGroup","UnityEngine.UI.HorizontalLayoutGroup","UnityEngine.UI.HorizontalOrVerticalLayoutGroup","UnityEngine.UI.LayoutElement","UnityEngine.UI.LayoutGroup","UnityEngine.UI.VerticalLayoutGroup","UnityEngine.UI.Mask","UnityEngine.UI.MaskableGraphic","UnityEngine.UI.RawImage","UnityEngine.UI.RectMask2D","UnityEngine.UI.Scrollbar","UnityEngine.UI.ScrollRect","UnityEngine.UI.Slider","UnityEngine.UI.Text","UnityEngine.UI.Toggle","UnityEngine.EventSystems.BaseInputModule","UnityEngine.EventSystems.EventSystem","UnityEngine.EventSystems.PointerInputModule","UnityEngine.EventSystems.StandaloneInputModule","UnityEngine.EventSystems.TouchInputModule","UnityEngine.EventSystems.Physics2DRaycaster","UnityEngine.EventSystems.PhysicsRaycaster"]

Deserializers.unityVersion = "2023.1.22f1";

Deserializers.productName = "Rap Idle Tycoon";

Deserializers.lunaInitializationTime = "09/14/2026 17:09:17";

Deserializers.lunaDaysRunning = "1.6";

Deserializers.lunaVersion = "7.2.0";

Deserializers.lunaSHA = "ea08d29afe2968efcb8d91d5624f033c6485cc68";

Deserializers.creativeName = "";

Deserializers.lunaAppID = "40536";

Deserializers.projectId = "875e7fe1e67864140ab9ad795a2712a8";

Deserializers.packagesInfo = "com.unity.nuget.newtonsoft-json: 3.2.2\ncom.unity.textmeshpro: 3.0.9\ncom.unity.timeline: 1.8.2\ncom.unity.ugui: 1.0.0";

Deserializers.externalJsLibraries = "";

Deserializers.androidLink = ( typeof window !== "undefined")&&window.$environment.packageConfig.androidLink?window.$environment.packageConfig.androidLink:'Empty';

Deserializers.iosLink = ( typeof window !== "undefined")&&window.$environment.packageConfig.iosLink?window.$environment.packageConfig.iosLink:'Empty';

Deserializers.base64Enabled = "False";

Deserializers.minifyEnabled = "True";

Deserializers.isForceUncompressed = "False";

Deserializers.isAntiAliasingEnabled = "False";

Deserializers.isRuntimeAnalysisEnabledForCode = "False";

Deserializers.runtimeAnalysisExcludedClassesCount = "2077";

Deserializers.runtimeAnalysisExcludedMethodsCount = "4587";

Deserializers.runtimeAnalysisExcludedModules = "physics3d, physics2d, particle-system, prefabs, mecanim-wasm";

Deserializers.isRuntimeAnalysisEnabledForShaders = "False";

Deserializers.isRealtimeShadowsEnabled = "False";

Deserializers.isLunaCompilerV2Used = "False";

Deserializers.companyName = "DefaultCompany";

Deserializers.buildPlatform = "StandaloneWindows64";

Deserializers.applicationIdentifier = "com.DefaultCompany.2DProject";

Deserializers.disableAntiAliasing = true;

Deserializers.graphicsConstraint = 24;

Deserializers.linearColorSpace = true;

Deserializers.buildID = "86adc738-f9f8-40fb-b0f5-654c13aa34b2";

Deserializers.runtimeInitializeOnLoadInfos = [[["UnityEngine","Experimental","Rendering","ScriptableRuntimeReflectionSystemSettings","ScriptingDirtyReflectionSystemInstance"]],[["DG","Tweening","DOTween","RuntimeOnLoad"],["Unity","VisualScripting","RuntimeVSUsageUtility","RuntimeInitializeOnLoadBeforeSceneLoad"]],[["$BurstDirectCallInitializer","Initialize"],["$BurstDirectCallInitializer","Initialize"],["$BurstDirectCallInitializer","Initialize"],["$BurstDirectCallInitializer","Initialize"],["$BurstDirectCallInitializer","Initialize"],["$BurstDirectCallInitializer","Initialize"],["$BurstDirectCallInitializer","Initialize"],["$BurstDirectCallInitializer","Initialize"]],[],[["Spine","Unity","AttachmentTools","AtlasUtilities","Init"],["OrientationTracking","Core","OrientationTracker","ResetStatic"],["Amanotes","Core","GameConfigManager","ResetStatic"],["Amanotes","Core","Timer","ResetStatic"],["Amanotes","Core","EventBus","ResetStatic"]]];

Deserializers.typeNameToIdMap = function(){ var i = 0; return Deserializers.types.reduce( function( res, item ) { res[ item ] = i++; return res; }, {} ) }()

