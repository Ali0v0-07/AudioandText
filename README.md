# AudioandText
现在主要实现了在C#环境下连接科大讯飞api的功能，使用websocket连接服务端，使用语音识别中的语音听写功能。
该项目是根据官方文档提供的Python示例demo改写而成。

具体详见官网 https://www.xfyun.cn/doc/asr/voicedictation/API.html#%E6%8E%A5%E5%8F%A3%E8%AF%B4%E6%98%8E

暂时仅支持导入音频，音频格式同官网描述，仅支持如下格式：

  1.pcm（pcm_s16le），wav，speex(speex-wb)
  
  2.采样率为16000 或者 8000. 推荐使用16000，比特率为16bit
  
  3.单声道
  
详见 https://www.xfyun.cn/doc/asr/voicedictation/Audio.html
