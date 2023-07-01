#!/bin/zsh

GEN_CLIENT=Luban.ClientServer/Luban.ClientServer.dll

dotnet ${GEN_CLIENT} -j cfg --\
 -d Defines/__root__.xml \
 --input_data_dir Datas \
 --output_code_dir Client/OutCodes \
 --output_data_dir Client/OutBytes \
 --gen_types code_cs_bin,data_bin \
 -s all 
