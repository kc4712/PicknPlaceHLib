# -*- coding: utf-8 -*-
"""
Created on Fri Feb 28 16:46:54 2020

@author: ABC
"""

import socket
import time

HOST = "192.168.1.2"
PORT = 30002
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.connect((HOST,PORT))
tx=293.41/1000
ty=140.87/1000
tz=322.05/1000
rx=3.221
ry=0.049
rz=0.061

output = "movep(p["+str(tx)+","+str(ty)+","+str(tz)+","+str(rx)+","+str(ry)+","+str(rz)+"], a=0.5, v=0.2)" + "\n"
s.sendall(output.encode('utf-8'))
time.sleep(2)
data = s.recv(1024)
s.close()
print("Received", repr(data))

