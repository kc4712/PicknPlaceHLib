"""Hand-eye calibration sample."""
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


app = zivid.Application()
    
SAVEDIRNAME = "./200302_1/"
SAVELOGDIRNAME = "./LOG200302_1/"
SAVELOGNAME = "200302_1"
current_pose_id = 0 
calibration_inputs = list()
LOGCNT = 0
import socket
import time




def _acquire_checkerboard_frame(camera):
    print("Capturing checkerboard image... ")
    # with camera.update_settings() as updater:
    #     updater.settings.iris = 16667
    #     updater.settings.brightness = 1.0
    #     updater.settings.gain = 1.0
    #     updater.settings.exposure_time = datetime.timedelta(microseconds=8333)
    #     updater.settings.filters.gaussian.enabled = True
    
    
    # settings_collection = [camera.settings for _ in range(2)]

    # settings_collection[0].iris = 17
    # settings_collection[0].exposure_time = datetime.timedelta(microseconds=16667)
    # settings_collection[0].brightness = 1
    # settings_collection[0].gain = 1
    # settings_collection[0].bidirectional = 0
    # settings_collection[0].filters.contrast.enabled = True
    # settings_collection[0].filters.Contrast.threshold = 5
    # settings_collection[0].filters.gaussian.enabled = True
    # settings_collection[0].filters.gaussian.sigma = 1.5
    # settings_collection[0].filters.outlier.enabled = True
    # settings_collection[0].filters.outlier.threshold = 5
    # settings_collection[0].filters.reflection.enabled = True
    # settings_collection[0].filters.saturated.enabled = True
    # settings_collection[0].blue_balance = 1.081
    # settings_collection[0].red_balance = 1.709

    # settings_collection[1].iris = 17
    # settings_collection[1].exposure_time = datetime.timedelta(microseconds=16667)
    # settings_collection[1].brightness = 1.8
    # settings_collection[1].gain = 1
    # settings_collection[1].bidirectional = 0
    # settings_collection[1].filters.contrast.enabled = True
    # settings_collection[1].filters.contrast.threshold = 5
    # settings_collection[1].filters.gaussian.enabled = True
    # settings_collection[1].filters.gaussian.sigma = 1.5
    # settings_collection[1].filters.outlier.enabled = True
    # settings_collection[1].filters.outlier.threshold = 5
    # settings_collection[1].filters.reflection.enabled = True
    # settings_collection[1].filters.saturated.enabled = True
    # settings_collection[1].blue_balance = 1.081
    # settings_collection[1].red_balance = 1.709
    
    suggest_setting_params = SuggestSettingsParameters(
        max_capture_time = datetime.timedelta(milliseconds=1200), 
        ambient_light_frequency = AmbientLightFrequency.hz60,
    )
    suggest_settings = zivid.capture_assistant.suggest_settings(camera, suggest_setting_params)
    print("OK")
    
    #return camera.capture(settings_collection)
    return zivid.hdr.capture(camera, suggest_settings)


def _enter_robot_pose(index):
    file = open('./pose.txt','r')
    lines = file.readlines()
    file.close()
    
    #inputted = input(
    #    str(index) + "번째 보정 진행중... 로봇 위치에 갔다면 Enter".format(
    #        index
    #    )
    #)
    print(str(index) + "번째 보정 진행중...")
     
    elements = lines[index].split(maxsplit=6)
    #elements = inputted.split(maxsplit=6)
    #data = np.array(elements, dtype=np.float64).reshape((4, 4))
    #robot_pose = zivid.hand_eye.Pose(data)
    print("기입한 로봇 자세:" + str(elements))
    #robot_pose = np.array(elements)
    robot_pose = np.asarray(elements, dtype='float64')

    translation = robot_pose[:3]
    rotation_vector = robot_pose[3:]
    rotation = Rotation.from_rotvec(rotation_vector)
    transform = np.eye(4)
    transform[:3, :3] = rotation.as_matrix()
    transform[:3, 3] = translation.T
    
    
    
    robot_pose = zivid.hand_eye.Pose(transform)
    print("The following pose was entered:\n{}".format(robot_pose))
    return robot_pose, transform


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


def _converting_cam_robot(camera_xyz):
    np.set_printoptions(precision=2)

    # Define (picking) point in camera frame
    #point_in_camera_frame = np.array([11.1, 39.9, 806.9, 1])
    #point_in_camera_frame = np.array([18.4, -31.0, 726.8, 1])
    point_in_camera_frame = np.array([-66.7, -104.2, 772.6, 1])
    point_in_camera_frame = camera_xyz
    
    robotstartPos = np.array([-96.63, 250.38, -36.3, 3.129, 0.041 , -0.002])
    
    robot_pose = np.asarray(robotstartPos, dtype='float64')

    translation = robot_pose[:3]
    rotation_vector = robot_pose[3:]
    rotation = Rotation.from_rotvec(rotation_vector)
    transform = np.eye(4)
    transform[:3, :3] = rotation.as_matrix()
    transform[:3, 3] = translation.T
    
    print(f"Point coordinates in camera frame: {point_in_camera_frame[0:3]}")

    # Check if YAML files are valid
    #_assert_valid_matrix(str(Path("./200220_1/eyeInHandTransform.yaml")))
    _assert_valid_matrix(str(Path(SAVEDIRNAME+"transformation.yaml")))

    # Reading camera pose in end-effector frame (result of eye-in-hand calibration)
    transform_end_effector_to_camera = _read_transform(
        str(Path(SAVEDIRNAME+"transformation.yaml"))
    )

    # Reading end-effector pose in robot base frame
    #transform_base_to_end_effector = _read_transform(str(Path("./200220_1/robotTransform.yaml")))

    # Computing camera pose in robot base frame
    #transform_base_to_camera = np.matmul(
    #    transform_base_to_end_effector, transform_end_effector_to_camera
    #)

    # Computing (picking) point in robot base frame
    #point_in_base_frame = np.matmul(transform_base_to_camera, point_in_camera_frame)
    po1 = np.matmul(transform_end_effector_to_camera, transform)
    
    file_storage = cv2.FileStorage(SAVEDIRNAME+"transformation.yaml", cv2.FILE_STORAGE_READ)
    transform = file_storage.getNode("PoseState").mat()
    file_storage.release()
    point_in_base_frame = np.matmul(transform, point_in_camera_frame)
    print(f"Point coordinates in robot base frame: {point_in_base_frame}")

def _get_mid_point(xyz):
    """Calculate mid point from average of the 100 centermost points.
    Args:
        xyz: X, Y and Z images (point cloud co-ordinates)
    Returns:
        mid_point: Calculated mid point
    """
    xyz_center_cube = xyz[
        int(xyz.shape[0] / 2 - 5) : int(xyz.shape[0] / 2 + 5),
        int(xyz.shape[1] / 2 - 5) : int(xyz.shape[1] / 2 + 5),
        :,
    ]
    return (
        np.nanmedian(xyz_center_cube[:, :, 0]),
        np.nanmedian(xyz_center_cube[:, :, 1]),
        np.nanmedian(xyz_center_cube[:, :, 2]),
    )


def _display_pointcloud(rgb, xyz):
    """Display point cloud.
    Display the provided point cloud `xyz`, and color it with `rgb`.
    We take the centermost co-ordinate as 'lookat' point. We assume that
    camera location is at azimuth -pi/2 and elevation -lpi/2 relative to
    the 'lookat' point.
    Args:
        rgb: RGB image
        xyz: X, Y and Z images (point cloud co-ordinates)
    """
    
    mid_point = _get_mid_point(xyz)
    point_cloud_to_view = xyz
    point_cloud_to_view[np.isnan(xyz[:, :, 2])] = 0
    viewer = pptk.viewer(point_cloud_to_view)
    viewer.attributes(rgb.reshape(-1, 3) / 255.0)
    viewer.set(lookat=mid_point)
    viewer.set(phi=-math.pi / 2, theta=-math.pi / 2, r=mid_point[2])
    


def isRotationMatrix(R):
    Rt = np.transpose(R)
    shouldBeIdentity = np.dot(Rt, R)
    I = np.identity(3, dtype = R.dtype)
    n = np.linalg.norm(I - shouldBeIdentity)
    return n < 1e-6

 




def _zivid_func(msg):
    global current_pose_id
    global pptk
    global calibration_inputs
    global LOGCNT
    global server
    print("get: " + msg)
    #print(list(msg.split(",")[0])[0]) 
    if (len(msg) > 2 and len(msg) > 0):
        data = msg.split(",")
        print(data)
    #print(data[2])
    #print(data[3])
    #print(chr(msg))
    calibrate = False
    if msg is "p":
        print("Detecting checkerboard square centers... ")
        try:
            camera = app.connect_camera()
            robot_pose, transform = _enter_robot_pose(current_pose_id)

            frame = _acquire_checkerboard_frame(camera)

            print("Detecting checkerboard square centers... ")
            result = zivid.hand_eye.detect_feature_points(frame.get_point_cloud())
            if result:
                print("OK")
                point_cloud = frame.get_point_cloud().to_array()
                xyz = np.dstack([point_cloud["x"], point_cloud["y"], point_cloud["z"]])
                rgb = np.dstack([point_cloud["r"], point_cloud["g"], point_cloud["b"]])
                _display_pointcloud(rgb, xyz)
                frame.save(SAVEDIRNAME+f"img{current_pose_id:02d}.zdf")
                file_storage = cv2.FileStorage(
                    str(SAVEDIRNAME+f"pos{current_pose_id:02d}.yaml"), cv2.FILE_STORAGE_WRITE
                )
                print(transform)
                file_storage.write("PoseState", transform)
                file_storage.release()

                res = zivid.hand_eye.CalibrationInput(robot_pose, result)
                calibration_inputs.append(res)
                current_pose_id += 1
            else:
                print("FAILED")
        except ValueError as ex:
            print(ex)
        camera.disconnect();
    elif msg == "c":
        #calibrate = True
        print("Performing hand-eye calibration...")
        calibration_result = zivid.hand_eye.calibrate_eye_to_hand(calibration_inputs)
        print(calibration_result)
        
        #code.interact(local=locals())
        if calibration_result:
            print("OK")
            transform = calibration_result.hand_eye_transform
            residuals = calibration_result.per_pose_calibration_residuals
            print("\n\nTransform: \n")
            np.set_printoptions(precision=5, suppress=True)
            print(transform)
        
            print("\n\nResiduals: \n")
            for res in residuals:
                print(f"Rotation: {res.rotation:.6f}   Translation: {res.translation:.6f}")
            
            file_storage_transform = cv2.FileStorage(
            str(SAVEDIRNAME+"transformation.yaml"), cv2.FILE_STORAGE_WRITE
            )
            file_storage_transform.write("PoseState", transform)
            file_storage_transform.release()
            file_storage_residuals = cv2.FileStorage(
                str(SAVEDIRNAME+"residuals.yaml"), cv2.FILE_STORAGE_WRITE
            )
            residual_list = []
            for res in residuals:
                tmp = list([res.rotation, res.translation])
                residual_list.append(tmp)
        
            file_storage_residuals.write(
                "Per pose residuals for rotation in deg and translation in mm",
                np.array(residual_list),
            )
            file_storage_residuals.release()
        else:
            print("FAILED")
        
    elif msg == "l":
        try:
            robot_pose, transform = _enter_robot_pose(current_pose_id)
            
            frame = zivid.Frame(SAVEDIRNAME+f"img{current_pose_id:02d}.zdf")
            #_acquire_checkerboard_frame(camera)

            print("Detecting checkerboard square centers... ")
            result = zivid.hand_eye.detect_feature_points(frame.get_point_cloud())
            if result:
                print("OK")
                point_cloud = frame.get_point_cloud().to_array()
                xyz = np.dstack([point_cloud["x"], point_cloud["y"], point_cloud["z"]])
                rgb = np.dstack([point_cloud["r"], point_cloud["g"], point_cloud["b"]])
                #if(pptk is not None):
                #    pptk.clear()
                        
                _display_pointcloud(rgb, xyz)
                file_storage = cv2.FileStorage(
                    str(SAVEDIRNAME+f"pos{current_pose_id:02d}.yaml"), cv2.FILE_STORAGE_WRITE
                )
                print(transform)
                file_storage.write("PoseState", transform)
                file_storage.release()

                res = zivid.hand_eye.CalibrationInput(robot_pose, result)
                calibration_inputs.append(res)
                current_pose_id += 1
                message = "Checker Done."
                #win32file.WriteFile(fileHandle, message.encode()+b'\n')
            else:
                print("FAILED")
                
        except ValueError as ex:
            print(ex)
            
    elif data[0] == 'm':
        data[0] = data[1]
        data[1] = data[2]
        data[2] = data[3]
        data[3] = data[4]
        data[4] = data[5]
        data[5] = data[6]
        data[6] = data[7]
        data[7] = data[8]
        #data[8] = data[9]
        #data[9] = data[10]
        
        
        print("socket")
        HOST = "192.168.1.2"
        PORT = 30002
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        
        s.connect((HOST,PORT))
        
        tx=float(data[0])/1000
        ty=float(data[1])/1000
        tz=float(data[2])/1000
        rx=data[3]
        ry=data[4]
        rz=data[5]
        a=data[6]
        v=data[7]
        output = "movel(p["+str(tx)+","+str(ty)+","+str(tz)+","+str(rx)+","+str(ry)+","+str(rz)+"], a="+str(a)+", v=" + str(v)+ ")" + "\n"
        s.sendall(output.encode('utf-8'))
        time.sleep(2)
        data = s.recv(1024)
        s.close()
        print("Received", repr(data))

        
        LOGCNT = LOGCNT + 1
            
            
    elif data[0] == 'v':
        # The Zivid3D.zdf file has to be in the same folder as this sample script.
        filename_zdf = "../Test.zdf"
    
        print(f"Reading {filename_zdf} point cloud")
        frame = zivid.Frame(filename_zdf)
    
        point_cloud = frame.get_point_cloud().to_array()
        xyz = np.dstack([point_cloud["x"], point_cloud["y"], point_cloud["z"]])
        rgb = np.dstack([point_cloud["r"], point_cloud["g"], point_cloud["b"]])
        
        
        #_display_pointcloud(rgb, xyz)
        
        
        #print(msg.split(" "))
        # print("data[0] == v")
        data[0] = data[1]
        data[1] = data[2]
        data[2] = data[3]
        data[3] = data[4]
        data[4] = data[5]
        data[5] = data[6]
        data[6] = 1
        # print(data)
        # Define (picking) point in camera frame
        
            
        #로봇의 trans축 변환을 위한 카메라 xyz데이터        
        point_in_camera_frame = np.array([float(data[0]), float(data[1]), float(data[2]), 1])
        
        
        
    
        # Check if YAML files are valid
        #_assert_valid_matrix(str(Path("./200220_1/eyeInHandTransform.yaml")))
        _assert_valid_matrix(str(Path(SAVEDIRNAME+"transformation.yaml")))
    
        # Reading camera pose in end-effector frame (result of eye-in-hand calibration)
        transform_end_effector_to_camera = _read_transform(
            str(Path(SAVEDIRNAME+"transformation.yaml"))
        )
    
        
        point_in_base_frame = np.matmul(transform_end_effector_to_camera, point_in_camera_frame)
        print(f"Point coordinates in robot base frame: {point_in_base_frame[0:3]}")




        #로봇의 회전 행렬 변환을 위한 카메라 xyz rxryrz 데이터 변경
        point_rot_in_camera_frame = np.array([float(data[0]), float(data[1]), float(data[2]), float(data[3]), float(data[4]), float(data[5])])
        #point_in_camera_frame = np.array([-66.7, -104.2, 772.6, 1])
        np.set_printoptions(precision=2)
        translation = point_rot_in_camera_frame[:3]
        rotation_vector = point_rot_in_camera_frame[3:]
        rotation = Rotation.from_rotvec(rotation_vector)
        transform = np.eye(4)
        transform[:3, :3] = rotation.as_matrix()
        transform[:3, 3] = translation.T
        
        point_rot_in_base_frame = np.matmul(transform_end_effector_to_camera, transform)
        rotMat = point_rot_in_base_frame[:3,:3]
        rotMatClass = Rotation.from_matrix(rotMat)
        convertRotVec = rotMatClass.as_rotvec()
        print(f"Rotate coordinates in robot base frame: {convertRotVec}")
        

        
        point_in_base_frame[2] += 40.0
        print(str(point_in_base_frame))
        
        
        f = open(SAVELOGDIRNAME+SAVELOGNAME+"_LOG.txt",'a')
        
        data = str(LOGCNT) + " "+ str(point_in_base_frame[0])+" "+ str(point_in_base_frame[1])+ " " + str(point_in_base_frame[2])+ "\n"
        f.write(data)
        f.close()
        frame.save(SAVELOGDIRNAME+f"img{LOGCNT:03d}.zdf")
        #if(int(data[0]) != 0 and int(data[1]) != 0 and int(data[2]) != 0):
        print("socket")
        HOST = "192.168.1.2"
        PORT = 30002
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        s.connect((HOST,PORT))
        tx=point_in_base_frame[0]/1000
        ty=point_in_base_frame[1]/1000
        tz=point_in_base_frame[2]/1000
        rx=convertRotVec[0]
        ry=convertRotVec[1]
        rz=convertRotVec[2]
        
        output = "movel(p["+str(tx)+","+str(ty)+","+str(tz)+","+str(rx)+","+str(ry)+","+str(rz)+"], a=0.2, v=0.1)" + "\n"
        print(output)
        s.sendall(output.encode('utf-8'))
        time.sleep(2)
        data = s.recv(1024)
        s.close()
        print("Received", repr(data))

        
        LOGCNT = LOGCNT + 1
        
    else:
        print("Unknown command '{}'".format(msg))
        #server.quit = -1
        #server.shutdown()
        #rpcThread._Thread__stop()
    test = msg + " ok"
    return test
    
        
def _rpcThread():    
    server.serve_forever();

server = SimpleXMLRPCServer(("localhost", 55556))
print("listening on port 55555")
server.register_function(_zivid_func, "msg")    

def _main():
    try:
        os.mkdir(SAVEDIRNAME)
    except FileExistsError:
        print("Dir Exist")
    try:
        os.mkdir(SAVELOGDIRNAME)
    except FileExistsError:
        print("Dir Exist")
    rpcThread = threading.Thread(target = _rpcThread, args=())
    rpcThread.start()
    #while(True):
        
    print("Program End")



if __name__ == "__main__":
    _main()