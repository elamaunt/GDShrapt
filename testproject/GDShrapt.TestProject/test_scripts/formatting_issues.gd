extends Node
class_name FormattingTest

## Test script with intentional formatting issues.
## Used to test the Format Code command.

# Variables with bad spacing around type annotations and assignments
var a:int=10
var b :String= "test"
var c : float =3.14
var d:Array[int]= [1,2,3]
var e :Dictionary={"key":"value","count":42}

# Exported variables with spacing issues
@export var speed:float=100.0
@export var name_value :String ="Player"

# Signals with bad formatting
signal value_changed(new_value:int)
signal data_updated(key:String,value:Variant)


func bad_spacing(x:int,y:int,z:int)->int:
	var result=x+y+z
	if result>10:
		return result*2
	else:
		return result


func inconsistent_operators():
	var a=10+5  # 32:5-GDL211-OK
	var b = 20-10  # 33:5-GDL211-OK
	var c=30 * 2  # 34:5-GDL211-OK
	var d = 40/ 4  # 35:5-GDL211-OK
	var e=a+b-c*d  # 36:5-GDL211-OK
	return e


func missing_spaces_in_comparisons(value:int)->bool:
	if value>100 and value<200:
		return true
	elif value>=50 or value<=10:
		return false
	elif value==75:
		return true
	elif value!=0:
		return false
	return value>0


func array_formatting():
	var arr=[1,2,3,4,5]
	var dict={"a":1,"b":2,"c":3}
	var nested=[{"x":1},{"y":2}]
	return [arr,dict,nested]


func function_call_spacing():
	var result=bad_spacing(1,2,3)
	print("Result:",result)
	var values=[1,2,3]
	values.append(4)
	return result


func long_line_test(parameter_one: int, parameter_two: String, parameter_three: float, parameter_four: Array, parameter_five: Dictionary) -> Dictionary:  # 67:0-GDL101-OK
	return {"param1": parameter_one, "param2": parameter_two, "param3": parameter_three, "param4": parameter_four, "param5": parameter_five}  # 68:0-GDL101-OK


func ternary_formatting(condition:bool)->int:
	return 10 if condition else 20


func lambda_formatting():
	var arr=[1,2,3,4,5]
	var filtered=arr.filter(func(x):return x>2)  # 77:40-GD3020-OK
	var mapped=arr.map(func(x):return x*2)
	return [filtered,mapped]


func match_statement_formatting(value:int)->String:
	match value:
		0:
			return"zero"
		1:
			return"one"
		2,3,4:
			return"small"
		_:
			return"other"


func for_loop_formatting():
	for i in range(10):
		print(i)
	for key in{"a":1,"b":2}:
		print(key)
	for item in[1,2,3]:
		print(item)


func string_formatting():
	var name="World"
	var count=42
	var msg1="Hello, "+name+"!"
	var msg2="Count: "+str(count)
	return[msg1,msg2]
