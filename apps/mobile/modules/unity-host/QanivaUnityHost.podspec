Pod::Spec.new do |s|
  s.name         = 'QanivaUnityHost'
  s.version      = '0.1.0'
  s.summary      = 'Qaniva native RN <-> Unity-as-a-Library transport (iOS)'
  s.description  = <<-DESC
    React Native bridge module that hosts the Unity simulation runtime.
    Compiles with or without UnityFramework present (__has_include guarded):
    without it, isUnityAvailable() reports false and startUnity() rejects.
  DESC
  s.homepage     = 'https://github.com/ranuelyn/qaniva'
  s.license      = { :type => 'Proprietary' }
  s.author       = 'Qaniva'
  s.platforms    = { :ios => '15.1' }
  s.source       = { :path => '.' }
  s.source_files = 'ios/**/*.{h,m,mm}'
  s.requires_arc = true

  s.dependency 'React-Core'
end
