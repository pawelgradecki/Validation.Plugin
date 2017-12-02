# Validation.Plugin

Simple plugin that prevents saving record if any of the required fields is not filled. You should register this plugin on Pre-Validation of Create and Update of entity which you want to protect (also register PreImage for Update message with name "PreImage").