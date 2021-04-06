/*
v1.0
This code contains works from unknown sources and is partially written by me
*/

#include <I2Cdev.h>
#include <MPU6050_6Axis_MotionApps20_edited.h>
#include <Wire.h>
#include <SoftwareSerial.h>

SoftwareSerial bt(4, 3);
MPU6050 mpu;

int MPUOffsets[6] = {-4263, -4711, 1833, 0, 1, 2};

uint8_t mpuIntStatus;
uint16_t packetSize;
uint16_t fifoCount;
uint8_t fifoBuffer[64];

volatile bool mpuInterrupt = false;

Quaternion q;
VectorFloat gravity;
float ypr[3];
float Yaw, Pitch, Roll;

String str;

void setup() {
  Wire.begin();
  bt.begin(9600);

  delay(1000);
  mpu.initialize();
  mpu.dmpInitialize();

  mpu.setXAccelOffset(MPUOffsets[0]);
  mpu.setYAccelOffset(MPUOffsets[1]);
  mpu.setZAccelOffset(MPUOffsets[2]);
  mpu.setXGyroOffset(MPUOffsets[3]);
  mpu.setYGyroOffset(MPUOffsets[4]);
  mpu.setZGyroOffset(MPUOffsets[5]);

  mpu.setDMPEnabled(true);
  
  attachInterrupt(0, dmpDataReady, RISING);
  mpuIntStatus = mpu.getIntStatus();
  packetSize = mpu.dmpGetFIFOPacketSize();
  
  delay(1000);
  mpu.resetFIFO();
  mpuInterrupt = false;
}

void loop() {
  if (mpuInterrupt ) {
    mpuInterrupt = false;
    fifoCount = mpu.getFIFOCount();
    
    if ((!fifoCount) || (fifoCount % packetSize)) {
      mpu.resetFIFO();
    } else {
      while (fifoCount  >= packetSize) {
        mpu.getFIFOBytes(fifoBuffer, packetSize);
        fifoCount -= packetSize;
      }
      
      mpu.dmpGetQuaternion(&q, fifoBuffer);
      mpu.dmpGetGravity(&gravity, &q);
      mpu.dmpGetYawPitchRoll(ypr, &q, &gravity);
      Yaw = (ypr[0] * 180.0 / M_PI) + 180;
      Pitch = (ypr[1] *  180.0 / M_PI) + 180;
      Roll = (ypr[2] *  180.0 / M_PI) + 180;

      str = String((long)Yaw) + " " + String((long)Pitch) + " " + String((long)Roll);
      bt.println(str);
    }
  }
}

void dmpDataReady() {
  mpuInterrupt = true;
}
