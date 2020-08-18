# -*- coding: utf-8 -*-
"""
Created on Tue Apr  7 13:47:51 2020

@author: ABC
"""

import datetime
import code
import zivid
import os
import cv2
import numpy as np
import scipy
import pptk
import math
from pathlib import Path
import win32api, win32pipe, win32file
import threading
from xmlrpc.server import SimpleXMLRPCServer
from xmlrpc.server import SimpleXMLRPCRequestHandler
import sys;

from scipy.spatial.transform import Rotation
from zivid.capture_assistant import AmbientLightFrequency, SuggestSettingsParameters
import zivid.hand_eye
import socket
import time


#zivid api초기화
#app = zivid.Application()
    
SAVEDIRNAME = "./200417_1/"
SAVELOGDIRNAME = "./LOG200408_1/"
SAVELOGNAME = "200417_1"


# The Zivid3D.zdf file has to be in the same folder as this sample script.
filename_zdf = "../Test.zdf"

print(f"Reading {filename_zdf} point cloud")
# C#에서 피사체를 촬영하여 기록한 3D 이미지 파일로 현재 무엇을 찍었는지 디스플레이
# C# ZIVID API 미비로 인한 임시 대처
#frame = zivid.Frame(filename_zdf)

#point_cloud = frame.get_point_cloud().to_array()
#xyz = np.dstack([point_cloud["x"], point_cloud["y"], point_cloud["z"]])
#rgb = np.dstack([point_cloud["r"], point_cloud["g"], point_cloud["b"]])


#_display_pointcloud(rgb, xyz)

def _assert_valid_matrix(file_name):
    """Check if YAML file is valid.
    Args:
        file_name: Path to YAML file.
    Raises:
        FileNotFoundError: If the YAML file specified by file_name cannot be opened.
        NameError: If the transformation matrix named 'PoseState' is not found in the file.
        ValueError: If the dimensions of the transformation matrix are not 4 x 4.
    """

    file_storage = cv2.FileStorage(file_name, cv2.FILE_STORAGE_READ)
    if not file_storage.open(file_name, cv2.FILE_STORAGE_READ):
        file_storage.release()
        raise FileNotFoundError(f"Could not open {file_name}")

    pose_state_node = file_storage.getNode("PoseState")

    if pose_state_node.empty():
        file_storage.release()
        raise NameError(f"PoseState not found in file {file_name}")

    shape = pose_state_node.mat().shape
    if shape[0] != 4 or shape[1] != 4:
        file_storage.release()
        raise ValueError(
            f"Expected 4x4 matrix in {file_name}, but got {shape[0]} x {shape[1]}"
        )


def _read_transform(file_name):
    """Read transformation matrix from a YAML file.
    Args:
        file_name: Path to the YAML file.
    Returns:
        transform: Transformation matrix.
    """

    file_storage = cv2.FileStorage(file_name, cv2.FILE_STORAGE_READ)
    transform = file_storage.getNode("PoseState").mat()
    file_storage.release()

    return transform





#############################################################################

#C#에게서 받은 피사체의 중심 POINTCLOUD 좌표를 파싱, 0.0.3과 다르게 로드리게즈 좌표로 받음####################
#print(msg.split(" "))
# print("data[0] == v")


np.set_printoptions(precision=2)


data = [212.564,-170.092,996.568,2.11147,2.01318,0.00782581]
data = [212.599,-170.131,996.621,-1.35773,1.21948,0.976697]
data = [212.558,-170.098,996.659,-0.162344,-1.72965,0.0862086]
data = [8.08696,20.3298,962.985,-1.19498,1.00202,-1.30724]
data = [-190.923,-1.8165,917.412,-0.128227,-1.72428,0.0609031]
data = [7.31753,20.3019,962.932,-0.101942,-0.153997,0.0599325]
#data = [-191.021,-162.843,930.663,-0.136567,-0.157302,-0.0202515]
#data = [212.592,-170.112,996.623,-0.154953,-0.162404,-0.0474239]
#data = [-191.195,-1.80803,917.339,-0.119659,-0.155748,-0.0409411]
#data = [214.523,-170.615,997.365,-0.0352396,-0.168689,-0.0106418]


#data = [-215.69,-139.99,925.447,0.0640203,2.98231,-0.0629798]
#data = [-215.666,-140.023,925.495,-0.244495,0.0652213,3.08846]
#data = [-216.063,-139.85,925.605,-0.0418135,-0.15743,-0.0427817]
#data = [173.429,-138.787,987.991,-0.206565,0.0897636,2.36215]
#data =[14.7703,-36.429,963.946,-0.0708451,-0.184584,0.364587]
data = [90.8405,43.3341,969.688,-0.026965,-0.230421,-0.488875]
data = [6.8398, 44.2535, 956.013, -0.0898055, -0.140087, 0.874171]


"""
data[0] = data[1]
data[1] = data[2]
data[2] = data[3]
data[3] = data[4]
data[4] = data[5]
data[5] = data[6]
data[6] = 1
"""
# print(data)
#########################################################################

    
        
point_in_camera_frame = np.array([float(data[0]), float(data[1]), float(data[2]), 1])
#Numpy의 부동소수점 정밀도 변경(소수점 자릿수 몇개 쓸거냐 하는 수준...)
np.set_printoptions(precision=2)
print(f"Point coordinates in camera frame: {point_in_camera_frame[0:3]}")

# Check if YAML files are valid
#_assert_valid_matrix(str(Path("./200220_1/eyeInHandTransform.yaml")))
_assert_valid_matrix(str(Path(SAVEDIRNAME+"transformation.yaml")))

# Computing camera pose in robot base frame
transform_end_effector_to_camera = _read_transform(
    str(Path(SAVEDIRNAME+"transformation.yaml"))
)

transform_end_effector_to_camera1 = _read_transform(
    str(Path("./200408_1/transformation.yaml"))
)



print(str(transform_end_effector_to_camera))
print("\n")
print(str(transform_end_effector_to_camera1))
# Computing (picking) point in robot base frame
point_in_base_frame = np.matmul(transform_end_effector_to_camera, point_in_camera_frame)

#피사체의 중심을 로봇좌표로 변환하기 때문에 로봇프로그래밍을 하지 않는 다면 변환된 좌표로 이동시 피사체를 짓눌러버린다.
#임의로 변환된 z축에 40mm을 더해 피사체를 짓누를 위험을 낮춘다.
#point_in_base_frame[2] += 40.0
print(str(point_in_base_frame))
print(f"Point coordinates in robot base frame: {point_in_base_frame[0:3]}")


#할콘에서 c#에게 주는 피사체 좌표는 ur의 로드리게즈 좌표로 변환해서 이곳으로 전달된다, 합성 연산시 ur과 피사체 좌표계를 일치시킨다 
#c# x y z rx ry rz 데이터 전달
#카메라의 좌표를 회전행렬로 변환
point_rot_in_camera_frame = np.array([float(data[0]), float(data[1]), float(data[2]), float(data[3]), float(data[4]), float(data[5])])
#point_in_camera_frame = np.array([-66.7, -104.2, 772.6, 1])
translation = point_rot_in_camera_frame[:3]
rotation_vector = point_rot_in_camera_frame[3:]
rotation = Rotation.from_rotvec(rotation_vector)
transform = np.eye(4)
transform[:3, :3] = rotation.as_matrix()
transform[:3, 3] = translation.T

#로봇 베이스 회전행렬과 카메라가 찍은 피사체의 회전행렬간 행렬곱
point_rot_in_base_frame = np.matmul(transform_end_effector_to_camera, transform)
#합성된 4X4 회전행렬에서 trans축을 제외 3x3회전 행렬만을 추출
rotMat = point_rot_in_base_frame[:3,:3]
rotMatClass = Rotation.from_matrix(rotMat)

#회전행렬 클래스에서 rotate vector로 변환 - 즉 로봇 회전좌표로 변환
convertRotVec = rotMatClass.as_rotvec()
print(f"Rotate coordinates in robot base frame: {convertRotVec}")


#f = open(SAVELOGDIRNAME+SAVELOGNAME+"_LOG.txt",'a')

data = str(0) + " "+ str(point_in_base_frame[0])+" "+ str(point_in_base_frame[1])+ " " + str(point_in_base_frame[2])+ " " + str(convertRotVec[0])+ " " + str(convertRotVec[1])+ " " + str(convertRotVec[2])+ "\n"
#f.write(data)
#f.close()
#frame.save(SAVELOGDIRNAME+f"img{LOGCNT:03d}.zdf")
#if(int(data[0]) != 0 and int(data[1]) != 0 and int(data[2]) != 0):



#로봇과의 통신 부분
print("socket")
HOST = "192.168.1.2"
PORT = 30002
#s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
#s.connect((HOST,PORT))
tx=point_in_base_frame[0]/1000
ty=point_in_base_frame[1]/1000
tz=point_in_base_frame[2]/1000

#로봇의 회전좌표 추가
rx=convertRotVec[0]
ry=convertRotVec[1]
rz=convertRotVec[2]

output = "movel(p["+str(tx)+","+str(ty)+","+str(tz)+","+str(rx)+","+str(ry)+","+str(rz)+"], a=0.2, v=0.1)" + "\n"