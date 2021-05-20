# Audio2Net
A networked audio application to send audio from one PC to another with extremely low latency.

To have your PC not output any audio from the speakers, use the following software:
https://vb-audio.com/Cable/index.htm

After installing, use Audio2Net with the `--list-outputs` argument, and pass the output you want as i.e `--output 1`

To decrease the latency, use `--max-latency`. Over a hard-wired internet connection, max latency at `0.03` is just good enough without any distortion: `--max-latency 0.03`

To boost the volume from the client to the server, pass `--volume-boost 1.1` for 110%, or `--volume-boost 5` for 500%. This will increase the volume of the audio coming from the server and play louder on the PC receiving the audio.

To let the server control the volume, pass `--server-volume` as a parameter. You can use the device running the server to control the volume, simply by turning up or down the volume in windows.

